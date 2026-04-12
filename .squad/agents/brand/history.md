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
