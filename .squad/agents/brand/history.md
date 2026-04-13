# Brand — Project History

## Project Context

- **Project:** Waymarked — UK walking, hiking, and running route planner
- **Requested by:** Josh Hills
- **Goal:** Build a route planner app from scratch, primarily for UK use. Customisable options, map view of routes. Starting point: find suitable data source(s).
- **Stack:** TBD — no tech decisions made yet.
- **Status:** Team hired, work beginning.

## Key Files

- `infra/graphhopper/docker-compose.yml` — GraphHopper container setup
- `infra/graphhopper/config.yml` — Routing profiles (foot, hike), elevation config
- `infra/graphhopper/README.md` — Setup instructions, memory requirements, API examples
- `infra/graphhopper/test-route.sh` — Bash test script (Edinburgh route)
- `infra/graphhopper/test-route.ps1` — PowerShell test script (Edinburgh route)

## Learnings

### GraphHopper Dockerfile (Local Build) (2026-04-12)

**Why no official image:**
- GraphHopper does NOT publish to Docker Hub or GHCR; `graphhopper/graphhopper:latest` is a 404
- Official distribution is via JAR on Maven Central; self-hosting means building your own image

**Dockerfile approach:**
- Base: `eclipse-temurin:21-jre` (GraphHopper 11.x requires Java 17+; 21 is current LTS)
- JAR downloaded via `ADD` instruction from Maven Central at build time (no auth needed)
- ENTRYPOINT uses `sh -c` with `exec` to allow `$JAVA_OPTS` expansion from environment variable
- Pattern: `ENTRYPOINT ["sh", "-c", "exec java ${JAVA_OPTS} -jar ... \"$@\"", "sh"]` + `CMD ["--config", "..."]`
- No `USER` instruction to avoid Podman user namespace conflicts

**Aspire integration:**
- `AddDockerfile(name, contextPath)` replaces `AddContainer(name, image)` when no prebuilt image exists
- Aspire builds the image locally on first `aspire run`; subsequent runs use cached image
- `contextPath` is relative to the AppHost project directory

### GraphHopper Docker Setup (2026-04-12)

**Configuration Patterns:**
- GraphHopper uses a YAML config file for routing profiles, elevation providers, and bounds
- Profiles: `foot` (general walking on roads + footpaths) vs `hike` (prefers trails, avoids roads, uses SAC scale)
- Elevation: SRTM auto-downloads from NASA (90m resolution), but OS Terrain 50 is superior for UK (50m, but manual setup)
- UK bounding box: `49,-8,61,2` (lat_min,lon_min,lat_max,lon_max) speeds up graph processing

**Memory Requirements:**
- Scotland extract (~200MB): 2-3GB Java heap (`-Xmx2g`), ~4GB total RAM needed
- Great Britain full (~1.5GB): 8-12GB Java heap (`-Xmx8g`), ~16GB total RAM needed
- Graph build is CPU and disk I/O intensive; SSD strongly recommended
- Insufficient heap causes `OutOfMemoryError` during graph creation

**Graph Build Times:**
- Scotland: 5-10 minutes on modern hardware
- Full GB: 20-40 minutes
- First run: OSM parsing + graph building + elevation download
- Subsequent runs: Fast startup if graph cache exists in `./data/graph-cache/`

**Profile Behavior Differences:**
- **`foot`:** Routes on road network + footpaths; faster routes, suitable for urban/mixed terrain
- **`hike`:** Strongly prefers dedicated hiking paths/trails; avoids roads; uses OSM `sac_scale` for difficulty rating
- Both profiles support elevation-aware routing with SRTM
- Round-trip routing supported natively (unlike Valhalla)

**Docker Gotchas:**
- Healthcheck needs `start_period: 300s` for initial graph build (startup can take 20-40 min for GB extract)
- Elevation download can fail/stall if NASA servers are slow; tiles cached in `elevation-cache/` after first success
- Config changes require deleting graph cache and rebuilding
- Port 8989 is default; healthcheck endpoint at `/health`

**API Features:**
- Points encoded by default (encoded polyline); use `points_encoded=false` for plain JSON coordinates
- Elevation in responses: `elevation=true` parameter
- Turn-by-turn instructions: `instructions=true&locale=en-GB`
- Alternative routes: `algorithm=alternative_route&alternative_route.max_paths=3`
- Round-trip: `round_trip.distance=10000&round_trip.seed=0`

### .NET Aspire Project Scaffolding (2026-04-12)

**Project Structure:**
- Used `dotnet new aspire-starter` template to scaffold initial solution
- .NET SDK 10.0.201 with Aspire workload installed (13.1.0-preview.1)
- Removed auto-generated `Waymarked.Web` Blazor frontend (not needed)
- Renamed `Waymarked.ApiService` → `Waymarked.Api` for consistency
- Created `Waymarked.Routing` class library for GraphHopper client abstraction

**Solution Layout:**
```
src/
  Waymarked.AppHost/          - .NET Aspire orchestrator (DistributedApplication)
  Waymarked.Api/              - ASP.NET Core Web API (minimal APIs)
  Waymarked.ServiceDefaults/  - Shared Aspire defaults (health checks, telemetry)
  Waymarked.Routing/          - GraphHopper HTTP client + models
```

**GraphHopper Container Integration:**
- AppHost configures GraphHopper as a container resource using `AddContainer()`
- Bind mounts: `config.yml` (read-only) and `data/` (read-write for OSM + graph cache)
- HTTP endpoint exposed on port 8989
- Java heap configured via `JAVA_OPTS` environment variable (`-Xmx2g -Xms2g`)
- API project references GraphHopper endpoint via Aspire's service discovery (`WithReference()`)

**GraphHopper Client Pattern:**
- Strongly-typed `HttpClient` wrapper: `GraphHopperClient`
- Request/response models: `RouteRequest`, `RouteResponse`, `RoutePath`, `RoutePoints`, `RouteInstruction`
- JSON deserialization using System.Text.Json with `[JsonPropertyName]` attributes
- DI registration via extension method: `AddGraphHopperClient(baseAddress)`
- BaseAddress configured from Aspire connection strings (`ConnectionStrings:graphhopper`)

**API Endpoint:**
- `POST /api/routes` — minimal API endpoint for route planning
- Accepts: `{ "from": [lat, lon], "to": [lat, lon], "profile": "hike" }`
- Returns: GraphHopper route response with distance, time, elevation, points (GeoJSON), instructions
- Error handling: Returns HTTP 502 Bad Gateway if routing engine is unreachable

**Package Dependencies:**
- `Waymarked.Routing` NuGet packages:
  - `Microsoft.Extensions.Http` 10.0.5 (HttpClient factory)
  - `Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.5
  - `Microsoft.Extensions.Logging.Abstractions` 10.0.5
- `Waymarked.Api` references `Waymarked.Routing` and `Waymarked.ServiceDefaults`
- `Waymarked.AppHost` SDK: `Aspire.AppHost.Sdk/13.1.0-preview.1`

**Build Warnings (Non-Blocking):**
- `ASPDEPR002`: `WithOpenApi()` is deprecated in .NET 10 (consider removing or replacing)
- `ASPIRE004`: ServiceDefaults project referenced by AppHost but not executable (expected, can set `IsAspireProjectResource="false"` to suppress)

**Aspire Connection String Pattern:**
- GraphHopper endpoint reference is passed to API via Aspire's service discovery
- API reads connection string: `builder.Configuration.GetConnectionString("graphhopper")`
- Aspire automatically injects the HTTP endpoint URL at runtime

**Data Directory Setup:**
- Created `infra/graphhopper/data/.gitkeep` to track empty directory
- OSM extracts and graph cache will be stored in `data/` (gitignored)
- Bind mount is read-write so GraphHopper can build/cache graph on first run

### Round-Trip Routing Support (2026-04-12)

**GraphHopper Round-Trip Mode:**
- GraphHopper supports native round-trip routing via `algorithm=round_trip` query parameter
- Parameters: `round_trip.distance` (metres), `round_trip.seed` (random integer for route variety)
- Only requires a single `point=` (start location); no destination needed
- Distance is specified in metres; seed controls route generation randomness

**API Design — Distance Unit Conversion:**
- Client sends distance in km or miles via `DistanceUnit` field ("kilometres" or "miles")
- API layer converts to metres before passing to `GraphHopperClient`:
  - 1 km = 1000 m
  - 1 mile = 1609.344 m
- Internal `RouteRequest.Distance` is always in metres (double?)
- Separation of concerns: client-facing units vs internal representation

**Model Changes:**
- Added `DistanceUnit` enum: `Kilometres | Miles` (in `RouteRequest.cs`)
- Made `RouteRequest.To` optional (`double[]?`) to support single-point round trips
- Added `RouteRequest.Distance` (nullable double, in metres)
- Added `RouteRequest.DistanceUnit` property (kept for reference, conversion happens in API layer)

**GraphHopperClient Branching:**
- Client now branches on `request.To == null`:
  - **A→B mode (To is NOT null):** Existing behavior — append `&point={To}`, no algorithm param
  - **Round-trip mode (To IS null):** Use `algorithm=round_trip`, `round_trip.distance`, `round_trip.seed`
- Uses `Random.Shared.Next()` for seed generation (different route each request)
- Logs round-trip mode activation with distance for debugging

**API Request Model:**
- Created `ApiRouteRequest` record in `Program.cs`:
  - `From: double[]` (required)
  - `To: double[]?` (optional)
  - `Distance: double?` (client units, km or miles)
  - `DistanceUnit: string?` (default "kilometres")
  - `Profile: string?` (default "hike")
- Validation:
  - `From` must be non-null and contain at least 2 elements (lat, lon)
  - If `To` is null, `Distance` must be > 0 (round-trip requires distance)
- Returns `Results.ValidationProblem` with clear messages on validation failure

**Backward Compatibility:**
- A→B routing unchanged: existing clients sending `From` + `To` work exactly as before
- Round-trip is opt-in: only triggered when `To` is omitted

### WaymarkedRouteResponse and Distance Validation (2026-04-12)

**WaymarkedRouteResponse Type:**
- Added new record type in `Waymarked.Routing` namespace
- Provides client-friendly derived fields from GraphHopper's raw response
- Properties:
  - `Distance` (double): raw metres from GraphHopper
  - `DistanceKm` (double): metres ÷ 1000, rounded to 2 decimal places
  - `DistanceMiles` (double): metres ÷ 1609.344, rounded to 2 decimal places
  - `DurationMs` (long): milliseconds from GraphHopper (already in ms, no conversion needed)
  - `DurationFormatted` (string): human-readable format (e.g., "1h 23m" or "45m")
  - `Points` (RoutePoints?): GeoJSON LineString from first path
  - `Instructions` (RouteInstruction[]?): turn-by-turn instructions
  - `IsRoundTrip` (bool): whether this was a round-trip request
- Factory method: `FromRouteResponse(RouteResponse response, bool isRoundTrip)`
- Duration formatting: shows hours only if >= 60 minutes, always shows minutes

**Distance Bounds Validation:**
- Validates round-trip distance AFTER unit conversion (in metres)
- Minimum: 500 metres (0.5 km)
- Maximum: 100,000 metres (100 km)
- Only validates when `distanceInMetres.HasValue` (A→B routes unaffected)
- Returns `Results.ValidationProblem` with descriptive error messages
- Validation happens post-conversion, so bounds are consistent regardless of input unit

**Coordinate Validation Placeholder:**
- TODO comment added in `Program.cs` before route request construction
- Reserved space for Data (GIS team) to add UK bounding box validation
- Keeps validation concerns separated: distance (Backend) vs coordinates (Data)

### Round-Trip Distance Accuracy Investigation (2026-04-13)

**Root Cause:**
GraphHopper's round-trip algorithm is probabilistic — it cannot guarantee returned distance matches requested distance. Three gaps in the old implementation: no `round_trip.max_retries` set (defaulted to 3), no client-side deviation check, and a single fixed seed per request.

**What Was Fixed:**
- Added `round_trip.max_retries=10` to every round-trip GraphHopper query (up from default 3)
- Added client-side retry loop: if returned distance deviates >15% from requested, retry with a new random seed (up to 3 retries = 4 total attempts)
- Added `MaxRetries` and `DistanceTolerance` to `RouteRequest`; API sets MaxRetries=3 for round-trip, 0 for A→B
- Exposed `distanceTolerance` as an optional API request parameter (default 0.15)
- Extracted `SendAsync` private method to avoid duplicating HTTP logic in the retry loop
- 4 new unit tests: max_retries in query, full retry exhaustion, early-stop on tolerance met, seed diversity

**Distance Unit Audit:**
No bugs found. Conversion (km/miles → metres) and validation (500m–100km) were already correct.

**What Requires Data's Input:**
- OSM path network density — sparse networks prevent long loops; client retries can't fix data gaps
- GraphHopper routing profile weight tuning in config.yml (hike profile)
- Elevation-corrected distance is a product decision (network km ≠ effort km in hilly terrain)

**Tests:** 38 Waymarked.Routing.Tests passing (was 34). Committed: 8dd8051
