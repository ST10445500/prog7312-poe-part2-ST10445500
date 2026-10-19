# Smart-X

> **Student Number:** ST10445500

---

## Table of Contents

- [What is Smart-X?](#what-is-smart-x)
- [Prerequisites](#prerequisites)
- [Restore and Build](#restore-and-build)
- [Running the App](#running-the-app)
- [Finding the Features](#finding-the-features)
- [Testing the Queues and Dictionaries](#testing-the-queues-and-dictionaries)
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

The PoE has three architectural pillars that switch on across the three parts. Part 1
built the first. This part builds the second, which makes the gateway react to
readings as they arrive instead of only storing them. The last pillar is still on the
start page, disabled, so the navigation is already in place for it.

| Pillar | State |
|---|---|
| Sensor Data Ingestion and Telemetry | Active, from Part 1 |
| Real-Time Command Stream and History | Active, new in Part 2 |
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
- **New:** watch every live device on one dashboard, with signal strength, protocol
  and hop count beside each reading
- **New:** let a critical alert jump the queue of ordinary readings
- **New:** open or close a pump or valve by hand, shut every open valve at once, and
  undo it
- **New:** look back through a device's history on a timeline
- **New:** stop a repeated fault or disconnect being alerted again and again
- **New:** get suggested actions based on what has happened on the stream
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

Part 2 adds one package, `Microsoft.AspNetCore.SignalR.Client`, to the web project.
`dotnet restore` fetches it like the rest.

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

The main way to run it is Visual Studio, which starts everything with one key. Docker
is the alternative if you would rather not install anything, and plain `dotnet run`
is there as a fallback.

### With Visual Studio

Open `SmartX.slnx` and pick the **App & Web** launch profile, then press F5. That
starts the API, the dashboard and the simulator together, and Shift+F5 stops all
three. The first time the API is served over https on a machine, the ASP.NET
development certificate has to be trusted:

```bash
dotnet dev-certs https --trust
```

| URL | What it is |
|---|---|
| `https://localhost:7267` | The dashboard. Open this one. |
| `https://localhost:7002` | The gateway API |
| `https://localhost:7002/openapi/v1.json` | The OpenAPI document |

The gateway starts with a small demo facility already registered, so the fleet is
never empty. **Readings only start arriving once the simulator is running**, so the
dashboards stay quiet until it is started.

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

This needs the same trusted development certificate as Visual Studio. Then three
terminals, from the repository root:

```bash
dotnet run --project SmartX.Api          # terminal 1, the gateway
dotnet run --project SmartX.Web          # terminal 2, the dashboard
dotnet run --project SmartX.Simulator    # terminal 3, the devices
```

The URLs are the same as the Visual Studio ones above.

### What the simulator does

On its own the simulator registers twenty more devices on top of the nine the gateway
starts with, so there are 28 devices reporting. It backfills a few hours of history
per sensor and then keeps streaming about a reading a second. I did not want anyone
to need a flag to see something happen, so each device has its own behaviour and
you should see all of these within about a minute:

- soil sensors that dry out and raise a low moisture alert
- power meters that surge past 600W
- radios that fade below -90 dBm
- valves that report a fault, such as `StuckValve` or `LowBattery`
- nodes that go quiet for a while and come back
- a flood of 300 readings every 75 seconds (the first at about 20 seconds), followed
  by one very dry moisture reading, so a critical alert has a queue to jump

The flags are there if you want to repeat one of these on demand:

| Flag | What it does |
|---|---|
| `--critical <mac>` | After 5 seconds, floods the gateway and then sends 5% moisture from that sensor |
| `--fault <mac>` | That valve reports `StuckValve` on every reading |
| `--drop <mac>` | That device goes quiet 10 seconds in, for 40 seconds, then comes back |

Both routes can run at the same time. Docker uses ports 8080 and 8081 and
`dotnet run` uses 7002 and 7267, so nothing collides.

---

## Finding the Features

**Ingestion** in the top navigation opens the overview dashboard at `/ingestion`.
That is the landing page for the whole Part 1 pillar: how many sensors are registered,
how many are reporting normally, which ones spiked or went quiet, and the total fleet
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

### Part 2: the Command Stream page

**Command Stream** in the top navigation opens `/commandstream`. It fills from the
simulator, so start that first.

**The live dashboard.** Each device is a row with its latest reading, its signal
strength, protocol and hop count, and when it was last heard. A badge at the top says
**Live** while the gateway is pushing changes to the browser and **Polling** if it has
fallen back to asking once a second. The tiles show the queue depth, active faults and
quiet devices, and *More stats* shows the rest. The search and filters are kept when
you go to a Part 1 page and come back.

**Search and filter.** The search box matches part of a MAC address. The category
select narrows to one kind of device and *Alerts only* keeps the devices with an
active fault. They work together.

**Manual overrides.** Each valve row has Open and Close buttons, and the filled one
shows where the valve is. **Shut all valves** closes every one that is open as a single
override. The gateway holds the command and the simulator's valves collect it before
their next reading, so a closed valve stays closed.

**Undo.** **Undo** reverts the newest override. Undoing a shut all puts back only the
valves that were open. The list under the buttons shows what can still be undone,
newest first, and an empty undo says there is nothing to undo.

**History timeline.** Click a device row to open its history underneath. It draws the
readings against the time they were taken, with buttons for the last minute, the last
five minutes or everything kept. A node that went quiet shows as a long flat gap.

**Suggested actions.** The panel beside the table lists devices that need attention
first, such as one with a fault or one that stopped reporting, then what usually
followed the last thing that happened. Each suggestion shows how sure it is and why,
and has a button where there is something to do, such as opening a valve.

**When the gateway goes away.** Stop the API and the badge drops to *Polling*, a red
banner says the gateway cannot be reached and the override buttons are disabled. It
recovers by itself when the API comes back.

---

## Testing the Queues and Dictionaries

Each of these needs the app running with the simulator. If you used Docker, use the
`8080` address in place of `7002` in the commands.

**1. A critical alert jumping the queue (priority queue and FIFO queue)**

The plain simulator does this about 20 seconds in and every 75 seconds after. Open
Command Stream, press *More stats* and watch:

1. **Queue depth** sits near zero, then jumps when the simulator floods the gateway
   with 300 readings.
2. A moisture sensor then reports 5%, below the 15% limit. **Alerts raised** goes up,
   and **Packets jumped** goes up by the number of ordinary readings the alert went
   ahead of.

To do it on demand:

```bash
dotnet run --project SmartX.Simulator -- --critical AA:BB:CC:DD:EE:05
```

**2. The undo stack**

1. Press **Open** on a valve, then **Shut all valves**. The undo list has two entries.
2. Press **Undo**. Only the valves the shut all closed come back, and one entry is left.
3. Press **Undo** twice more. The last press says there is nothing to undo and does
   not crash.

**3. Duplicate faults and disconnects (hash sets)**

1. Run `dotnet run --project SmartX.Simulator -- --fault AA:BB:CC:DD:EE:06`. That
   valve reports `StuckValve` on every reading.
2. Its fault chip appears once and is counted once. **Suppressed** goes up with every
   repeat after that, which is the set turning away a fault it already has.
3. Run `--drop AA:BB:CC:DD:EE:06` instead. After ten seconds the device shows under
   **Disconnected** and clears when it comes back. Repeat disconnects are counted too.

**4. The device dictionary and the sorted history**

1. Click any row. The history opens, and the line stays in time order even though the
   simulator sends some readings late.
2. Ask the API directly. The MAC is the key, in any case:

```bash
curl -k "https://localhost:7002/api/stream/devices?search=ee:05"
curl -k "https://localhost:7002/api/stream/devices/aa:bb:cc:dd:ee:05/history"
```

**5. The suggestion engine**

1. Type `aa:bb:cc:dd:ee:05` into the search box and leave it for a couple of seconds.
2. The panel offers **Open valve-b1** and says how often that followed the search. The
   engine starts with a small made-up history, which is where this comes from.
3. Click it. The valve opens and the override shows in the undo list.

Devices with a fault or that stopped reporting are listed as problems straight away.

---

## Final Architecture

The client is a standalone app, not hosted inside the API:

```text
Browser
  |
  v
SmartX.Web  (Blazor WebAssembly dashboard, runs in the browser)
  |  calls via HttpClient / JSON and one SignalR connection, allowed back by CORS
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

### How a reading travels in Part 2

```text
Device or simulator
  |  POST /api/telemetry
  v
TelemetryController  checks the reading against the alert rules
  |-- new alert ------> AlertQueue   (priority queue)
  `-- anything else --> PacketQueue  (ordinary queue)
                           |
                           v
StreamProcessor  takes alerts first, then packets, and fills
  DeviceCache (latest state by MAC), HistoryLog (sorted by time)
  and ActiveFaults (hash sets), then tells the browser via SignalR
```

The check happens in the controller, not the processor, because an alert that waited
until the processor would already be stuck behind the queue it was meant to jump. If
the device already has that fault, the reading goes down the ordinary path and is
counted as a repeat instead of alerted again.

The suggestions come from `InsightService`, which counts which event followed which
(an alert, an override or a search) and offers the most common follower of whatever
happened last, with a percentage for how often it did.

### Projects in the Solution

| Project | What it is |
|---|---|
| `SmartX.Api` | ASP.NET Core Web API on .NET 10. The gateway, and the only thing that stores telemetry. |
| `SmartX.Web` | Blazor WebAssembly client on .NET 10. Talks to the API over HTTP and SignalR. |
| `SmartX.Shared` | Models and validation rules both sides need, so there is one copy of each. |
| `SmartX.Simulator` | Console app that acts like a fleet of devices and posts readings to the API. |
| `SmartX.Tests` | xUnit tests over the Part 2 collections, the stream processor and the suggestion engine. |

---

## Tech Stack

| Layer | Technology | Why I Used It |
|---|---|---|
| **Backend** | ASP.NET Core Web API (`.NET 10`) | The brief mandates a .NET 10 backend. I used controllers rather than minimal APIs because grouping the routes by controller kept each file small enough to read. |
| **Frontend** | Blazor WebAssembly (`.NET 10`) | The frontend was free choice. I picked Blazor so the whole solution stays C# and the client can share model classes with the API instead of me redefining them in another language. |
| **Live updates** | SignalR | Polling alone made the page feel a second behind. The gateway now tells the page when something changed, and polling stays as the fallback. |
| **Shared models** | A class library both projects reference | I used this so a change to a model is a compile error on both sides straight away, rather than something I only find at runtime. |
| **Styling** | Bootstrap 5.3.3, vendored into `wwwroot` | I used Bootstrap for the grid and form styling, and copied it into the project instead of using a CDN so the dashboard still renders with no internet. |
| **Storage** | In-memory, with hand-written and built-in collections | The rubric wants specific structures, and telemetry is a stream where only the recent window matters, so memory fits the problem better than a database would. |
| **Priority queue** | The built-in `PriorityQueue`, inside my own locked class | I kept the built-in one and spent the effort on the parts around it: equal alerts leaving in the order they arrived, and sharing it safely between requests and the processor. |
| **Encryption** | `System.Security.Cryptography` AES-256 | The file upload criterion asks for full encryption. I streamed it in 64KB chunks so a large upload is never held in memory all at once. |
| **Device simulation** | Console app posting over HTTP | I needed a fleet to look at without owning any hardware, and posting through the real endpoint means the simulator is not a special case in the API. |
| **Testing** | xUnit | I used xUnit because it is the standard .NET test framework and it is what the test project template gives you. |
| **Containerisation** | Docker and Docker Compose | I used Compose so the gateway, dashboard and simulator start together with one command, and so a marker does not have to trust a certificate or open three terminals. |

---

## How the Brief Maps to the Code

The brief names specific language features and data structures it wants demonstrated.
This is where each one lives, so they are easy to find:

### Part 1

| Requirement | Where it lives | How it works |
|---|---|---|
| **Generics, without boxing** | `SmartX.Api/Models/Telemetry/TelemetryPacket.cs` | `TelemetryPacket<T>` is a `readonly struct` constrained `where T : struct`. The constraint is the point: it keeps the payload on the stack instead of boxing every reading onto the heap. |
| **Operator overloading** | `MoistureReading.cs`, `PowerReading.cs` | `PowerReading` overloads `+` and `-` so the fleet total is a sum of readings rather than of raw ints, and both types overload `>`, `<`, `==` and `!=` so the spike check compares two readings directly. |
| **Multi-dimensional arrays into `List<T>`** | `SmartX.Api/Collections/TelemetryBatches.cs` | A `TelemetryPacket<T>[,]` grid of `BatchSize = 50` by `MaxBatches = 20` collects raw readings. When the grid fills it transfers into a `List<T>` and resets, capped at `MaxTransferred = 5000`. |
| **Custom collection** | `SmartX.Api/Collections/RingBuffer.cs` | A fixed-size wrapping array implementing `IEnumerable<T>`. When it is full the oldest reading is overwritten, so memory never grows. `SensorTelemetry<T>` pairs it with the batch grid behind one lock. |
| **Recursion with a base case** | `SmartX.Api/Services/DeploymentTreeValidator.cs` | Walks the nested deployment tree, reporting unregistered MACs and duplicate sibling names. `MaxDepth = 32` stops the walk and reports an error rather than overflowing the stack. |
| **Encrypted upload** | `SmartX.Api/Services/AttachmentEncryption.cs` | AES-256 with a fresh IV per file written in front of the ciphertext, streamed in 64KB chunks. The key is validated at construction so a bad key fails at startup, not on first upload. |
| **Engagement feature** | `SmartX.Api/Services/TelemetryHealth.cs`, `SmartX.Web/Components/SignalRibbon.razor` | Splits readings into equal time windows and marks each normal, spike or silent. An empty window is a disconnect; a change over `PowerSpikeWatts = 50` or `MoistureSpikePercent = 15` is a spike. |

### Part 2

| Requirement | Where it lives | How it works |
|---|---|---|
| **Queue for incoming packets** | `SmartX.Api/Collections/PacketQueue.cs` | A `Queue<T>` behind one lock. The controller adds to the back and the processor takes from the front, both O(1). |
| **Priority queue for critical alerts** | `SmartX.Api/Collections/AlertQueue.cs` | The built-in `PriorityQueue`, so the most severe alert leaves first and equal ones leave in the order they arrived. O(log n). |
| **Alerts jump the queue** | `TelemetryController.cs`, `StreamProcessor.cs` | The controller puts an alert on the priority queue, and the processor empties that before it touches an ordinary packet. `StreamStats` counts the packets each alert went ahead of. |
| **Stack of overrides, for undo** | `SmartX.Api/Collections/CommandHistory.cs`, `SmartX.Api/Services/CommandService.cs` | A `Stack<T>` where each entry remembers the state every valve was in before, so one undo puts back a whole shut all. Push and pop are O(1). |
| **Dictionary of live devices** | `SmartX.Api/Services/DeviceCache.cs` | A `Dictionary` keyed on the upper-case MAC, so finding a device for each packet is O(1) whatever case it was sent in. |
| **Sorted dictionary of history** | `SmartX.Api/Collections/HistoryLog.cs` | A `SortedDictionary` per device keyed on the time of the reading, so the history stays in order however readings arrive. Two readings with the same time are both kept. Each device keeps its latest 500. |
| **Waiting commands** | `SmartX.Api/Collections/PendingCommands.cs` | A `Dictionary` by MAC holding the newest command a valve has not collected yet. |
| **Sets of faults and quiet devices** | `SmartX.Api/Collections/ActiveFaults.cs` | A `HashSet` of quiet devices and a `HashSet` of (MAC, fault code). `Add` says whether the entry was new, so a repeat is turned away and counted. O(1) to add or check. |
| **Pattern analysis** | `SmartX.Api/Collections/TransitionModel.cs` | A dictionary of counts for how often one event was followed by another. |
| **Suggestions** | `SmartX.Api/Services/InsightService.cs`, `SmartX.Web/Components/SuggestionsPanel.razor` | Lists problem devices, then the most common follower of the last event, with a percentage and a reason. |

**Thread safety.** Readings arrive from many devices while the processor and the page
read the same structures, so every shared structure uses a `lock` in every member, and
hands back a copy instead of the live collection. I used locks rather than concurrent
collections because some steps have to happen together, such as clearing a device's
old fault and raising a new one. The tests run the queues, the cache, the stack and
the sets from many threads at once.

The alert rules can be changed in the `Stream` section of
`SmartX.Api/appsettings.json`, for example `LowMoisturePercent` (15), `HighWatts` (600)
and `HistoryCap` (500). The API checks them at startup and needs a restart to pick up
a change.

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

The simulator runs with no flags in Compose, so all of its behaviours show up there
too. Because storage is in memory there are no volumes. Stopping the containers clears
the readings, which is the same thing that happens when the API restarts.

---

## Testing

Tests live in `SmartX.Tests` and use **xUnit**.

```bash
dotnet test SmartX.Tests/SmartX.Tests.csproj
```

**Latest result: 30/30 tests passing**

For Part 2 I kept the tests on the structures and the suggestion engine, since those
are the places a bug would be hardest to spot from the UI. The Part 1 test classes are
no longer in the project, so I checked Part 1 by running it.

| Test class | What it covers |
|---|---|
| `PacketQueueTests` | Packets coming out in the order they went in. |
| `AlertQueueTests` | The most severe alert leaving first, equal alerts keeping their order, and no loss with many threads. |
| `StreamProcessorTests` | An alert behind 200 packets being handled first, and a quiet device being marked disconnected. |
| `CommandHistoryTests` | The newest override being undone first. |
| `CommandServiceTests` | An empty undo returning cleanly, and undoing a shut all reopening the right valves. |
| `DeviceCacheTests` | Finding a device whatever the case of its MAC, and no loss with many threads. |
| `HistoryLogTests` | Sorted history from out-of-order readings, the oldest being dropped at the cap, same-time readings both kept, and range ends included. |
| `ActiveFaultsTests` | A repeat fault being turned away and counted, a fault being raised again once cleared, repeat disconnects counted, and one winner among many threads. |
| `TransitionModelTests` | The most common follower ranking first, and the percentages adding up. |
| `InsightServiceTests` | The seeded suggestions, a repeated pattern beating the seed, and faulty or quiet devices coming first. |

---

## Project Structure

```text
SmartX/
  SmartX.slnx                 Solution file, XML format
  SmartX.slnLaunch            Visual Studio multi-project launch profile
  docker-compose.yml

  SmartX.Api/                 The gateway
    Collections/              RingBuffer, TelemetryBatches, SensorTelemetry,
                              PacketQueue, AlertQueue, CommandHistory, HistoryLog,
                              ActiveFaults, PendingCommands, TransitionModel
    Controllers/              Sensors, Telemetry, Deployment, Attachments,
                              Stream, Commands, Insights
    Hubs/                     StreamHub, the live push to the browser
    Models/
      Telemetry/              TelemetryPacket, MoistureReading, PowerReading, DTOs
      Attachments/            Attachment metadata
      Stream/                 Packets, alerts, commands and settings for Part 2
    Services/                 Stores, encryption, validator, health, demo fleet,
                              DeviceCache, AlertRules, StreamProcessor,
                              CommandService, InsightService
    uploads/                  Encrypted attachments on disk
    Program.cs                Startup, DI, CORS, the hub, demo fleet seeding
    Dockerfile

  SmartX.Web/                 The dashboard
    Pages/
      Home.razor              Landing page with the three pillars
      Ingestion/              Overview, Fleet, Deployment, Files
      CommandStream/          The Part 2 dashboard
    Components/               SignalRibbon, Sparkline, StatTile, tree editor,
                              TimelineChart, SuggestionsPanel, UndoPanel
    Layout/                   Nav and main layout
    Services/                 Formatting, tree helpers, DashboardState
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

Every route is a controller action, plus one SignalR hub. `Program.cs` only calls
`MapOpenApi()` in Development, `MapControllers()` and the hub.

### Sensors

| Method | Route | What it does |
|---|---|---|
| `POST` | `/api/sensors` | Registers a sensor. Returns 409 if the MAC is already taken. |
| `GET` | `/api/sensors` | Lists every sensor registered with the gateway. |
| `GET` | `/api/sensors/{macAddress}` | Returns one sensor, or 404. |

### Telemetry

| Method | Route | What it does |
|---|---|---|
| `POST` | `/api/telemetry` | Records a single reading from a device. It also joins the live stream, and the signal fields and a fault code are optional. |
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

### Stream

| Method | Route | What it does |
|---|---|---|
| `GET` | `/api/stream/snapshot` | The queue depth, alert counts, faults and devices in one call. Takes `search`, `category` and `alertsOnly`. |
| `GET` | `/api/stream/devices` | The devices the stream has seen, with the same filters. |
| `GET` | `/api/stream/devices/{macAddress}/history` | A device's readings, oldest first. Takes `from` and `to`. 404 if it was never heard from. |

### Commands

| Method | Route | What it does |
|---|---|---|
| `POST` | `/api/commands` | Opens or closes one pump or valve. |
| `POST` | `/api/commands/close-all` | Closes every valve that is open, as one override. |
| `POST` | `/api/commands/undo` | Reverts the newest override. 409 if there is nothing to undo. |
| `GET` | `/api/commands/history` | The overrides that can still be undone, newest first. |
| `GET` | `/api/commands/pending/{macAddress}` | Hands a valve the command waiting for it. 204 if there is none. |

### Insights

| Method | Route | What it does |
|---|---|---|
| `GET` | `/api/insights` | The suggested actions, problems first. |
| `POST` | `/api/insights/search` | Tells the engine what was searched for. |

The live updates use a SignalR hub at `/hubs/stream`. The OpenAPI document is at
`/openapi/v1.json` when the API runs in Development.

---

## Notes for the Marker

- **The AES key ships in `SmartX.Api/appsettings.json` on purpose**, so attachments
  work the moment the project is cloned with nothing to configure. A real deployment
  would keep it in user secrets or an environment variable.
- **Zone and room are optional when a sensor is registered.** A device registers with
  a name and is then dragged into the deployment tree, which writes its zone and room
  back. That is why a new sensor shows as "Not placed" until it has been put
  somewhere.
- **Storage is in memory.** Restarting the gateway clears every reading, override and
  attachment and reseeds the demo fleet. That is a deliberate choice, since the brief
  is about the ingestion pipeline and the data structures rather than persistence.
- **The priority queue is the built-in one.** I wrapped `PriorityQueue` in my own class
  rather than writing a heap, and put the work into the ordering and the locking.
- **Overrides reach a valve when it next asks.** The gateway keeps the newest command
  for each valve until the valve collects it, so the valve answers within a second or
  two, and a switched-off valve keeps its command until it comes back.

---

## Demo

Video walkthrough: https://youtu.be/1GeMPZxtRro 

---

## AI Assistance Declaration

AI tools were used as support during the project, but I reviewed and adjusted the
final implementation and testing myself.

- **GitHub Copilot** was used to assist with the testing setup and to help define the
  scope of the automated tests. As well as which tests to prioritise.
- **ChatGPT** was used to assist with generating site style visuals and styling ideas
  for the dashboard and related frontend presentation.
- **ChatGPT** was used to assist with the formatting and style visuals of the README
  document.
- **ChatGPT** was used to assist with coming up with appropriate naming conventions for the custom data structures files and class names.

---
