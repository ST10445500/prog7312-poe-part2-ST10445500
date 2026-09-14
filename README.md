# Smart-X

> **Student Number:** ST10445500

---

## Table of Contents

- [What is Smart-X?](#what-is-smart-x)
- [Prerequisites](#prerequisites)
- [Restore and Build](#restore-and-build)
- [Running the App](#running-the-app)
- [Finding the Features](#finding-the-features)
- [Final Architecture](#final-architecture)
- [Tech Stack](#tech-stack)
- [How the Brief Maps to the Code](#how-the-brief-maps-to-the-code)
- [Docker and Containerisation](#docker-and-containerisation)
- [Testing](#testing)
- [Project Structure](#project-structure)
- [Key API Endpoints](#key-api-endpoints)
- [Notes for the Marker](#notes-for-the-marker)
- [Demo](#demo)
- [AI Assistance Declaration](#ai-assistance-declaration)

---

## What is Smart-X?

Smart-X is a telemetry gateway for IoT deployments spread across sites that do not
share a network, such as a hydroponic farm or a utility tracker. ESP32-class devices
post readings of several different types, and the gateway has to validate and store
all of them without falling over when a device misbehaves.

The awkward part of the scenario is that the readings are not all the same shape. A
moisture sensor sends a `float`, a power meter sends an `int`, and a valve sends a
`bool`. I did not want three near-identical pipelines, so the whole system is built
around one generic packet type that can carry any of them.

The PoE has three architectural pillars that switch on across the three parts. This
part builds the first. The other two appear on the start page, disabled, so the
navigation is already in place for later.

| Pillar | State |
|---|---|
| Sensor Data Ingestion and Telemetry | Active |
| Real-Time Command Stream and History | Part 2 |
| Network Topology and Mesh Routing | Part 3 |

What the gateway lets you do:

- Register sensors against a MAC address, a name and a category, and refuse a
  duplicate MAC
- Take readings one at a time or in batches, from the simulator or from the UI
- Arrange registered sensors into a nested deployment tree of zones and rooms, and
  validate that tree recursively before anything is saved
- Attach configuration files or photos to a sensor, encrypted with AES-256 on disk
- Watch each device's recent behaviour as a ribbon of time windows that shows spikes
  and disconnects as a break in the pattern
- Run the whole stack, gateway and dashboard and simulated devices, with one Docker
  command

---

## Prerequisites

| Requirement | Version | Check with |
|---|---|---|
| .NET SDK | 10.0 or later | `dotnet --version` |
| Docker Desktop | optional, only for the Docker route | `docker --version` |

Bootstrap 5.3.3 is vendored into `SmartX.Web/wwwroot/lib/bootstrap/` rather than
loaded from a CDN, so the dashboard still looks right with no internet connection.

---

## Restore and Build

From the repository root:

```bash
dotnet restore SmartX.slnx
dotnet build SmartX.slnx
dotnet test SmartX.Tests/SmartX.Tests.csproj
```

The solution file is `SmartX.slnx`, the newer XML solution format, not a `.sln`.

---

## Running the App

There are two ways to run it. Docker is the fewest steps, and `dotnet run` is the
better one if you want to put a breakpoint in the API.

### With Docker

Everything speaks plain HTTP inside Compose, so no development certificate is needed
at all. Make sure Docker Desktop is running, then from the repository root:

```bash
docker compose up --build     # first run, or after a code change
docker compose up             # after that
docker compose down           # stop and remove the containers
docker compose logs -f simulator
```

| URL | What it is |
|---|---|
| `http://localhost:8081` | The dashboard. Open this one. |
| `http://localhost:8080` | The gateway API |

The simulator waits a few seconds for the gateway to start listening before its
first call, so give the dashboard about twenty seconds to fill up.

### With dotnet run

The first time the API is served over https on a machine, the ASP.NET development
certificate has to be trusted. This is only needed for this route, not the Docker
one:

```bash
dotnet dev-certs https --trust
```

Then three terminals, from the repository root:

```bash
dotnet run --project SmartX.Api          # terminal 1, the gateway
dotnet run --project SmartX.Web          # terminal 2, the dashboard
dotnet run --project SmartX.Simulator    # terminal 3, the devices
```

| URL | What it is |
|---|---|
| `https://localhost:7267` | The dashboard. Open this one. |
| `https://localhost:7002` | The gateway API |
| `https://localhost:7002/openapi/v1.json` | The OpenAPI document |

Or open `SmartX.slnx` in Visual Studio and pick the **App & Web** launch profile,
which starts all three together.

The gateway starts with a small demo facility already registered, so the fleet is
never empty. **Readings only start arriving once the simulator is running**, so the
telemetry dashboard stays quiet until terminal 3 is started. The simulator backfills
a few hours of history per sensor and then keeps streaming about a reading a second.

Both routes can run at the same time. Docker uses ports 8080 and 8081 and
`dotnet run` uses 7002 and 7267, so nothing collides.

---

## Finding the Features

**Ingestion** in the top navigation opens the overview dashboard at `/ingestion`.
That is the landing page for the whole pillar: how many sensors are registered, how
many are reporting normally, which ones spiked or went quiet, and the total fleet
load. Everything below is one click from there, either on a card or on the strip
under the heading.

**Registering a sensor.** Fleet page. A device needs a MAC address, a name and a
category. The form validates the MAC against a pattern before anything is posted,
and the API turns away a duplicate MAC with a 409.

**The deployment hierarchy and the recursive validator.** Deployment page. A newly
registered sensor starts unplaced at the top and is dragged into a zone or room;
saving writes each sensor's location back from where it sits. Press
**Edit deployment**, then **JSON**, for two demo buttons: *Broken tree* posts a tree
that names a sensor the gateway has never seen, and *40 levels deep* posts one nested
past the validator's 32 level limit. Both come back as a list of errors instead of a
crash. That is what the depth guard is for.

**Encrypted file upload.** Device Files page. Pick a device and attach a
configuration file or a photo. It is encrypted with AES-256 on the way to disk and
decrypted on the way back out, so the copy sitting on the server is never readable.
Uploads are capped at 25MB on both sides, and downloading one confirms it
round-trips.

**Signal Trace, the engagement feature.** Fleet page, in the Signal Trace column.
Each device gets a ribbon of fixed time windows, oldest on the left. Both of the
failures worth catching show up as a break in the pattern:

- a **spike** window is hatched, for a sharp change between consecutive readings
- a **silent** window is drawn empty with a dashed outline, meaning nothing arrived
  at all

Colour is never the only cue, which is why one is hatched and the other is dashed.
Hover any block for the reading count and the largest change inside that window.

To make one happen on demand, use **Record a test reading** at the bottom of the
Fleet page. It posts through the same `/api/telemetry` endpoint the simulator and a
real device use. Post a wattage more than 50W away from the last one, or a moisture
value more than 15% away, and that window turns into a spike. Stop the simulator and
wait, and the newest windows go silent.

---

## Final Architecture

The client is a standalone app, not hosted inside the API:

```text
Browser
  |
  v
SmartX.Web  (Blazor WebAssembly dashboard, runs in the browser)
  |  calls via HttpClient / JSON, allowed back by CORS
  v
SmartX.Api  (ASP.NET Core Web API, the gateway)
  |
  v
In-memory stores and the hand-written collections
```

**Why the client is standalone:** hosting the Blazor app inside the API would have
been less work, but then the two could never be deployed separately, and the brief
treats the client and the backend API as separate layers. Keeping them apart meant I
had to deal with CORS properly: the API allows the client's origin, and the client's
`HttpClient` points at the API's base URL from
`SmartX.Web/wwwroot/appsettings.json`. That is the same problem a real deployment
would have, so it felt like the more honest way to build it.

### Projects in the Solution

| Project | What it is |
|---|---|
| `SmartX.Api` | ASP.NET Core Web API on .NET 10. The gateway, and the only thing that stores telemetry. |
| `SmartX.Web` | Blazor WebAssembly client on .NET 10. Talks to the API over HTTP. |
| `SmartX.Shared` | Models and validation rules both sides need, so there is one copy of each. |
| `SmartX.Simulator` | Console app that acts like a fleet of devices and posts readings to the API. |
| `SmartX.Tests` | xUnit tests over the hand-written collections and the recursive validator. |

---

## Tech Stack

| Layer | Technology | Why I Used It |
|---|---|---|
| **Backend** | ASP.NET Core Web API (`.NET 10`) | The brief mandates a .NET 10 backend. I used controllers rather than minimal APIs because grouping the routes by controller kept each file small enough to read. |
| **Frontend** | Blazor WebAssembly (`.NET 10`) | The frontend was free choice. I picked Blazor so the whole solution stays C# and the client can share model classes with the API instead of me redefining them in another language. |
| **Shared models** | A class library both projects reference | I used this so a change to a model is a compile error on both sides straight away, rather than something I only find at runtime. |
| **Styling** | Bootstrap 5.3.3, vendored into `wwwroot` | I used Bootstrap for the grid and form styling, and copied it into the project instead of using a CDN so the dashboard still renders with no internet. |
| **Storage** | In-memory, with hand-written collections | The rubric wants a custom data structure, and telemetry is a stream where only the recent window matters, so a fixed-size ring buffer fits the problem better than a database would. |
| **Encryption** | `System.Security.Cryptography` AES-256 | The file upload criterion asks for full encryption. I streamed it in 64KB chunks so a large upload is never held in memory all at once. |
| **Device simulation** | Console app posting over HTTP | I needed a fleet to look at without owning any hardware, and posting through the real endpoint means the simulator is not a special case in the API. |
| **Testing** | xUnit | I used xUnit because it is the standard .NET test framework and it is what the test project template gives you. |
| **Containerisation** | Docker and Docker Compose | I used Compose so the gateway, dashboard and simulator start together with one command, and so a marker does not have to trust a certificate or open three terminals. |

---

## How the Brief Maps to the Code

The brief names specific language features it wants demonstrated. This is where each
one lives, so they are easy to find:

| Requirement | Where it lives | How it works |
|---|---|---|
| **Generics, without boxing** | `SmartX.Api/Models/Telemetry/TelemetryPacket.cs` | `TelemetryPacket<T>` is a `readonly struct` constrained `where T : struct`. The constraint is the point: it keeps the payload on the stack instead of boxing every reading onto the heap. |
| **Operator overloading** | `MoistureReading.cs`, `PowerReading.cs` | `PowerReading` overloads `+` and `-` so the fleet total is a sum of readings rather than of raw ints, and both types overload `>`, `<`, `==` and `!=` so the spike check compares two readings directly. |
| **Multi-dimensional arrays into `List<T>`** | `SmartX.Api/Collections/TelemetryBatches.cs` | A `TelemetryPacket<T>[,]` grid of `BatchSize = 50` by `MaxBatches = 20` collects raw readings. When the grid fills it transfers into a `List<T>` and resets, capped at `MaxTransferred = 5000`. |
| **Custom collection** | `SmartX.Api/Collections/RingBuffer.cs` | A fixed-size wrapping array implementing `IEnumerable<T>`. When it is full the oldest reading is overwritten, so memory never grows. `SensorTelemetry<T>` pairs it with the batch grid behind one lock. |
| **Recursion with a base case** | `SmartX.Api/Services/DeploymentTreeValidator.cs` | Walks the nested deployment tree, reporting unregistered MACs and duplicate sibling names. `MaxDepth = 32` stops the walk and reports an error rather than overflowing the stack. |
| **Encrypted upload** | `SmartX.Api/Services/AttachmentEncryption.cs` | AES-256 with a fresh IV per file written in front of the ciphertext, streamed in 64KB chunks. The key is validated at construction so a bad key fails at startup, not on first upload. |
| **Engagement feature** | `SmartX.Api/Services/TelemetryHealth.cs`, `SmartX.Web/Components/SignalRibbon.razor` | Splits readings into equal time windows and marks each normal, spike or silent. An empty window is a disconnect; a change over `PowerSpikeWatts = 50` or `MoistureSpikePercent = 15` is a spike. |

---

## Docker and Containerisation

### Why Docker

The problem Docker solves here is the *"it works on my machine"* one. Running the
project by hand needs the right SDK, a trusted https certificate and three terminals
started in the right order. With Compose it is one command and none of that setup.

### My Docker Setup

```text
docker-compose.yml
  |-- api          -> ASP.NET Core gateway, port 8080
  |-- web          -> Blazor WebAssembly build served by nginx, port 8081
  `-- simulator    -> console app posting readings, no ports
```

| Service | Image build | Ports (host:container) | Notes |
|---|---|---|---|
| `api` | `SmartX.Api/Dockerfile` | `8080:8080` | Runs in Development so the OpenAPI document is served. |
| `web` | `SmartX.Web/Dockerfile` | `8081:80` | Built with the .NET SDK, then the static output is served by `nginx:alpine`. |
| `simulator` | `SmartX.Simulator/Dockerfile` | none | `depends_on` the api, then sleeps fifteen seconds before its first post. |

The containers find each other by service name on Compose's internal network. The
simulator posts to `http://api:8080/`, which only works inside the network. The
dashboard is different: the Blazor app runs in the browser, not in the container, so
it has to reach the API at `http://localhost:8080/` from outside. That is why the
api service sets `Cors__AllowedOrigins__0=http://localhost:8081`, to allow the
dashboard's origin back.

Because storage is in memory there are no volumes. Stopping the containers clears
the readings, which is the same thing that happens when the API restarts.

---

## Testing

Tests live in `SmartX.Tests` and use **xUnit**.

```bash
dotnet test SmartX.Tests/SmartX.Tests.csproj
```

**Latest result: 12/12 tests passing**

They cover the two pieces I wrote by hand, which are the two places a bug would be
hardest to spot from the UI:

| Test class | What it covers |
|---|---|
| `RingBufferTests` | Overwriting the oldest item when full, staying correct over several laps, enumerating in order after wrapping, and throwing on `Newest()` when empty. |
| `DeploymentTreeValidatorTests` | A valid tree passing, an unregistered sensor being reported, a tree exactly at the depth limit passing, and a tree past the limit stopping instead of overflowing. |
| `TelemetryHealthTests` | A reading landing in the right window, and the spike threshold flagging correctly. |
| `SensorTelemetryTests` | Concurrent `Add` calls keeping every reading, since the simulator posts from several devices at once. |
| `TelemetryBatchesTests` | Filling the whole batch grid transfers every reading into the list and resets the grid. |

---

## Project Structure

```text
SmartX/
  SmartX.slnx                 Solution file, XML format
  SmartX.slnLaunch            Visual Studio multi-project launch profile
  docker-compose.yml

  SmartX.Api/                 The gateway
    Collections/              RingBuffer, TelemetryBatches, SensorTelemetry
    Controllers/              Sensors, Telemetry, Deployment, Attachments
    Models/
      Telemetry/              TelemetryPacket, MoistureReading, PowerReading, DTOs
      Attachments/            Attachment metadata
    Services/                 Stores, encryption, validator, health, demo fleet
    uploads/                  Encrypted attachments on disk
    Program.cs                Startup, DI, CORS, demo fleet seeding
    Dockerfile

  SmartX.Web/                 The dashboard
    Pages/
      Home.razor              Landing page with the three pillars
      Ingestion/              Overview, Fleet, Deployment, Files
    Components/               SignalRibbon, Sparkline, StatTile, tree editor
    Layout/                   Nav and main layout
    Services/                 Formatting and tree helpers
    wwwroot/
      lib/bootstrap/          Bootstrap 5.3.3, vendored
    nginx.conf                Used by the Docker image only
    Dockerfile

  SmartX.Shared/
    Models/                   DTOs both sides use

  SmartX.Simulator/           Console app acting as the device fleet
    Dockerfile

  SmartX.Tests/               xUnit tests
```

---

## Key API Endpoints

Every route is a controller action. `Program.cs` only calls `MapOpenApi()` in
Development and `MapControllers()`.

### Sensors

| Method | Route | What it does |
|---|---|---|
| `POST` | `/api/sensors` | Registers a sensor. Returns 409 if the MAC is already taken. |
| `GET` | `/api/sensors` | Lists every sensor registered with the gateway. |
| `GET` | `/api/sensors/{macAddress}` | Returns one sensor, or 404. |

### Telemetry

| Method | Route | What it does |
|---|---|---|
| `POST` | `/api/telemetry` | Records a single reading from a device. |
| `POST` | `/api/telemetry/batch` | Records a batch and reports which readings were turned away. |
| `GET` | `/api/telemetry/{macAddress}` | Returns the recent readings for one sensor. |
| `GET` | `/api/telemetry/{macAddress}/usage` | Returns how full that sensor's storage structures are. |
| `GET` | `/api/telemetry/{macAddress}/health` | Splits behaviour into time windows. Takes `windows` (default 24) and `windowSeconds` (default 10). |
| `GET` | `/api/telemetry/overview` | Every sensor with its latest reading, for the overview table. |
| `GET` | `/api/telemetry/load` | Combined draw of every power meter on the fleet. |

### Deployment

| Method | Route | What it does |
|---|---|---|
| `GET` | `/api/deployment` | Returns the deployment tree the gateway is running. |
| `PUT` | `/api/deployment` | Replaces the tree, refusing one the validator turns down. |
| `POST` | `/api/deployment/validate` | Validates a tree without saving it. |

### Attachments

| Method | Route | What it does |
|---|---|---|
| `POST` | `/api/sensors/{macAddress}/attachments` | Uploads a file, encrypting it on the way to disk. Capped at 25MB. |
| `GET` | `/api/sensors/{macAddress}/attachments` | Lists the files attached to a sensor, newest first. |
| `GET` | `/api/sensors/{macAddress}/attachments/{id}` | Downloads one file, decrypting it on the way out. |
| `DELETE` | `/api/sensors/{macAddress}/attachments/{id}` | Removes the metadata and the encrypted copy on disk. |

The OpenAPI document is at `/openapi/v1.json` when the API runs in Development.

---

## Notes for the Marker

- **The AES key ships in `SmartX.Api/appsettings.json` on purpose**, so attachments
  work the moment the project is cloned with nothing to configure. A real deployment
  would keep it in user secrets or an environment variable.
- **Zone and room are optional when a sensor is registered.** A device registers with
  a name and is then dragged into the deployment tree, which writes its zone and room
  back. That is why a new sensor shows as "Not placed" until it has been put
  somewhere.
- **Storage is in memory.** Restarting the gateway clears every reading and
  attachment and reseeds the demo fleet. That is a deliberate choice for Part 1, not
  an oversight, since the brief is about the ingestion pipeline rather than
  persistence.

---

## Demo

Video walkthrough: *link to follow.*

---

## AI Assistance Declaration

AI tools were used as support during the project, but I reviewed and adjusted the
final implementation and testing myself.

- **GitHub Copilot** was used to assist with the testing setup and to help define the
  scope of the automated tests.
- **ChatGPT** was used to assist with generating site style visuals and styling ideas
  for the dashboard and related frontend presentation.
- **ChatGPT** was used to assist with the formatting and style visuals of the README
  document.

---
