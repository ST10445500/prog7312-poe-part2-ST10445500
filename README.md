# Smart-X

A hybrid IoT telemetry gateway for monitoring distributed environments such as
hydroponic farms, utility trackers and smart grids. ESP32-class devices publish
multi-typed telemetry to a central gateway that validates, stores and optimises
the stream.

The application is built around three pillars. This part of the project builds
the first one; the other two are shown on the start page but disabled.

| Pillar | State |
|---|---|
| Sensor Data Ingestion and Telemetry | Active |
| Real-Time Command Stream and History | Part 2 |
| Network Topology and Mesh Routing | Part 3 |

## Projects

| Project | What it is |
|---|---|
| `SmartX.Api` | ASP.NET Core Web API on .NET 10. The gateway, and the only thing that stores telemetry. |
| `SmartX.Web` | Blazor WebAssembly client on .NET 10. Talks to the API over HTTP. |
| `SmartX.Shared` | Models and validation rules both sides need, so there is one copy of each. |
| `SmartX.Simulator` | Console app that acts like a fleet of devices and posts readings to the API. |
| `SmartX.Tests` | xUnit tests over the hand written collections and the recursive validator. |

The client is a standalone app, not hosted inside the API. The two are linked by
CORS: the API allows the client's origin, and the client's `HttpClient` points at
the API's base URL from `SmartX.Web/wwwroot/appsettings.json`.

---

## Prerequisites

| Requirement | Version | Check with |
|---|---|---|
| .NET SDK | 10.0 or later | `dotnet --version` |
| Docker Desktop | optional, only for the Docker route | `docker --version` |

Bootstrap 5.3.3 is vendored into `SmartX.Web/wwwroot/lib/bootstrap/` rather than
loaded from a CDN, so the app still looks right with no internet connection.

---

## Restore and build

From the repository root:

```bash
dotnet restore SmartX.slnx
dotnet build SmartX.slnx
dotnet test SmartX.Tests/SmartX.Tests.csproj
```

---

## Running it

The first time the API is served over https on a machine, the ASP.NET
development certificate has to be trusted. This is only needed for the
`dotnet run` route below, not for the Docker one:

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

The gateway starts with a small demo facility already registered, so the fleet
is never empty. **Readings only start arriving once the simulator is running**,
so the telemetry dashboard stays quiet until terminal 3 is started. The
simulator backfills a few hours of history per sensor and then keeps streaming.

Or open `SmartX.slnx` in Visual Studio and pick the **App & Web** launch profile,
which starts all three together.

---

## Running it with Docker

Everything speaks plain HTTP inside Compose, so no development certificate is
needed at all. From the repository root:

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

Both routes can run at the same time. Docker uses ports 8080 and 8081, and
`dotnet run` uses 7002 and 7267, so nothing collides.

---

## Finding the features

**Ingestion** in the top navigation opens the overview dashboard at
`/ingestion`. That is the landing page for the whole pillar: how many sensors are
registered, how many are reporting normally, which ones spiked or went quiet, and
the total fleet load. Everything below is one click from there, either on a card
or on the strip under the heading.

**Registering a sensor.** Fleet page. A device needs a MAC
address, a name and a category. The form validates the MAC against a pattern
before anything is posted, and the API turns away a duplicate MAC with a 409.

**The deployment hierarchy and the recursive validator.** Deployment page. A
newly registered sensor starts unplaced at the top and is
dragged into a zone or room; saving writes each sensor's location back from
where it sits. Press **Edit deployment**, then **JSON**, for two demo buttons:
*Broken tree* posts a tree that names a sensor the gateway has never seen, and
*40 levels deep* posts one nested past the validator's 32 level limit. Both come
back as a clean list of errors rather than a crash, which is the point of the
depth guard.

**Encrypted file upload.** Device Files page. Pick a sensor and attach
a configuration file, deployment photo or hardware log. The file is encrypted
with AES-256 on its way to disk and decrypted on its way back out, so the copy
stored on the server is never readable. Uploads are capped at 25MB on both
sides. Download one to confirm it round-trips.

**Signal Trace, the engagement feature.** Fleet page, in the Signal Trace column,
and on the overview dashboard for anything misbehaving. Each sensor gets a
ribbon of fixed time windows, oldest to newest, so both of the failure modes
that matter show up as a break in one pattern:

- a **spike** window is hatched, for a sharp change between consecutive readings
- a **silent** window is drawn empty with a dashed outline, meaning nothing
  arrived at all

Colour is never the only cue. Hover any block for the reading count and the
largest change inside that window.

To make one happen on demand, use **Record a test reading** at the bottom of the
Fleet page. It
posts through the same `/api/telemetry` endpoint the simulator and a real device
use. Post a wattage more than 50W away from the last one, or a moisture value
more than 15% away, and that window turns into a spike. Stop the simulator and
wait, and the newest windows go silent.

---

## Notes for the marker

- **The AES key ships in `SmartX.Api/appsettings.json` on purpose**, so
  attachments work the moment the project is cloned with nothing to configure. A
  real deployment would keep it in user secrets or an environment variable.
- **Zone and room are optional when a sensor is registered.** A device registers
  with a name and is then dragged into the deployment tree, which writes its zone
  and room back. That is why a new sensor shows as "Not placed" until it has been
  put somewhere.
- **Storage is in memory.** Restarting the gateway clears every reading and
  attachment and reseeds the demo fleet.

## Demo

Video walkthrough: *link to follow.*
