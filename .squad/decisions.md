# Squad Decisions

## Active Decisions

### 2026-04-12: Routing Engine Selection

**Status:** LOCKED | **Owner:** Josh Hills | **Participants:** Mikey (Lead), Data (GIS Engineer)

#### Decision: Use GraphHopper as primary routing engine with Node.js/Express backend and React/Mapbox GL frontend

**Reasoning:**
GraphHopper is the optimal choice for UK walking/hiking/running route planning:
- **Apache 2.0 license** (permissive, commercial-friendly)
- **Hiking profiles** with native round-trip/circular route generation (hike.json)
- **Self-hostable** — Java-based, containerizable, infra costs reasonable (~€19/mo on Hetzner CX42)
- **Production-proven** — widely used for routing applications
- **Elevation-aware** — critical for hill/peak avoidance and scenic preferences
- **Route diversity support** — native circular/round-trip routing

**Architecture:**
- **Backend:** Node.js + Express (excellent GraphHopper API client bindings; Python alternative via FastAPI)
- **Frontend:** React + Mapbox GL JS (best vector tile performance, free tier 50k loads/month)
- **Deployment:** Docker-based GraphHopper on single VPS; scale horizontally as needed
- **Mapping data:** OSM via Mapbox vector tiles or self-hosted OpenMapTiles

**Route Customization Mapping:**
- **Surface type:** Custom costing weights for `surface` tags
- **Avoid roads:** Turn penalties on certain highway types
- **Avoid hills:** Grade/elevation-based penalties
- **Prefer scenic:** Weighted by terrain and surface characteristics
- **Waypoints:** Standard array support
- **Elevation profiles:** Via DEM integration (OS Terrain 50 for UK)
- **Round-trip/circular:** Native support via GraphHopper routing parameters

**Alternatives Considered:**
- **Valhalla:** MIT license (good), multi-modal, C++-based, lacks native round-trip support (would require custom algorithm)
- **OSRM:** BSD license (good), car-focused, limited pedestrian profiles, no elevation support
- **OpenRouteService:** GPL-3.0 (incompatible with commercial licensing), forked from GraphHopper

**Validation Tasks (Spike + Data):**
- Brand spike: Deploy GraphHopper with UK OSM extract, validate performance and customization
- Data validation: Verify UK OSM footpath coverage (highways, designations, surface tags, elevation data)

**Key Advantages:**
- **Round-trip routing:** Native support ✅ (advantage over Valhalla)
- **Hiking profiles:** Excellent out-of-box hiking profiles ✅
- **Cost:** Infra costs within reasonable bounds (~€19/mo)
- **Licensing:** Apache 2.0 ✅ commercial-friendly

**Next Steps:**
1. Brand: Spike GraphHopper deployment with UK OSM extract and custom hiking profiles
2. Data: Validate UK OSM footpath coverage and elevation data completeness
3. Backend: Integrate GraphHopper Node.js client bindings
4. Testing: Validate custom costing for user preferences (surface, hills, scenic routing)

---

### 2026-04-12: .NET Aspire Solution Architecture

**Status:** IMPLEMENTED | **Owner:** Brand (Backend Dev)

#### Decision: Scaffolded .NET Aspire solution with GraphHopper container resource

**Architecture:**
- **Solution:** `src/Waymarked.sln` (.NET 10, Aspire 13.1.0-preview.1)
- **Projects:**
  - `Waymarked.AppHost` — Aspire orchestrator
  - `Waymarked.Api` — ASP.NET Core Web API
  - `Waymarked.ServiceDefaults` — Shared Aspire service defaults
  - `Waymarked.Routing` — GraphHopper client library

**Namespace Convention:**
- Root namespace: `Waymarked` (project-first, no company prefix)
- Project suffixes: `.AppHost`, `.Api`, `.Routing`, `.ServiceDefaults`
- API project: `Waymarked.Api` (no "Service" suffix)

**GraphHopper Container Integration:**
- AppHost adds GraphHopper container (`graphhopper/graphhopper`) with:
  - Bind mount: config.yml (read-only) at `/data/config.yml`
  - Bind mount: OSM data directory (read-write) at `/data`
  - HTTP endpoint on port 8989
  - Java options: `-Xmx2g -Xms2g`
- API references GraphHopper via Aspire endpoint, waits for container readiness
- Service discovery via connection string: `builder.Configuration.GetConnectionString("graphhopper")`

**Routing Client Pattern:**
- Separate `Waymarked.Routing` class library with strongly-typed `GraphHopperClient`
- Request/response models: `RouteRequest`, `RouteResponse` with JSON mapping
- DI extension method: `AddGraphHopperClient(baseAddress)` for registration
- Minimal APIs endpoint: `POST /api/routes` with direct DI of `GraphHopperClient`

**Key Benefits:**
- Domain logic (routing) separated from API infrastructure ✅
- GraphHopper implementation swappable without API changes ✅
- Testable and reusable across services ✅
- Modern .NET 10 patterns (minimal APIs, expression-bodied methods) ✅

**Open Questions:**
- Health check implementation for GraphHopper connectivity
- Telemetry: custom metrics for route requests, failures, latency
- Retry policy: Polly for transient failure handling
- ServiceDefaults warning suppression (`ASPIRE004`)

**Next Steps:**
- Add OSM data download script/instructions
- Implement health check for GraphHopper connectivity
- Add integration tests for routing endpoint
- Consider `Aspire.Hosting.Testing` for AppHost tests

---

### 2026-04-12: Data Stack for UK Walking Route Planner

**Status:** Ready for Review | **Owner:** Data (GIS Engineer)

#### Decision: OpenStreetMap routing + OS Terrain 50 elevation + OpenTopoMap/Thunderforest tiles + Photon geocoding

**Routing Data:**
- **OpenStreetMap** — Excellent UK footpath/PROW coverage
  - Key tags: `highway=footway|path|bridleway|track`, `designation=public_footpath|public_bridleway`, `foot=yes|designated|permissive`, `surface=*`, `sac_scale=*`
  - Route relations: `type=route`, `route=hiking|foot`, `network=iwn|nwn|rwn|lwn`
  - ODbL licence: free for commercial use with attribution
  - MapThePaths project actively adding PROW data
  - Quality varies by region; generally good for established routes

**Elevation Data:**
- **OS Terrain 50** (primary) — 50m resolution, free, OGL, authoritative for UK (±2.5m RMSE)
- **Copernicus DEM 30m** (fallback for areas outside GB)
- **SRTM 90m** available but inferior to OS Terrain 50 for UK

**Basemap Tiles:**
- **MVP:** OpenTopoMap (free, topographic style, good for hiking, no SLA)
- **Production:** Thunderforest Outdoors ($29-500+/month, purpose-built for outdoor use, commercial SLA)
- **Premium option:** OS Maps API tiles as subscription feature for users wanting official OS Explorer style
- **At scale:** Self-hosted tiles rendering OSM (~$50-150/month VPS)

**Geocoding:**
- **MVP:** Photon (photon.komoot.io) or Nominatim public instance (free, 1 req/s rate limit)
- **Production:** Self-hosted Nominatim (~$50-100/month VPS for UK/Europe extract) OR commercial provider (LocationIQ, OpenCage, Geoapify ~$50-100/month)
- **UK premium option:** OS Names API for authoritative postcode/address lookup (~£100s/month)

**Routing Engine (per Data research):**
- **GraphHopper recommended** — Java-based, excellent hiking profiles, native elevation integration, customizable difficulty ratings, route diversity support
- Alternative engines: OSRM (fastest, car-focused), Valhalla (multi-modal, C++)
- *Note: Mikey's research recommends Valhalla; team to reconcile*

**UK-Specific Access Rights:**
- **England/Wales:** PROW system (`designation=public_footpath|public_bridleway|restricted_byway|byway_open_to_all_traffic`), permissive paths, Access Land (CRoW Act)
- **Scotland:** Right to Roam (Scottish Outdoor Access Code) — broader access rights, PROW concept less relevant
- **Seasonal closures:** Nesting (Mar-Aug), lambing (Mar-May), grouse shooting (Aug-Dec), military ranges, tidal access NOT in static datasets

**Data Integration Strategies:**
- Open PROW data (rowmaps.com) — 149 authorities released data under OGL; could supplement/verify OSM
- Future enhancement: Conflate PROW data into OSM; partner for real-time seasonal closure feeds
- User-generated reporting for path conditions/closures

**Cost Estimates:**
- **MVP stack:** ~$20-50/month (routing engine VPS only; all data free)
- **Production stack:** ~$150-400/month (routing, tiles, geocoding scaled)

**Licence Summary:**
| Source | Type | Licence | Commercial Use | Cost |
|--------|------|---------|-----------------|------|
| OpenStreetMap | Vector data | ODbL | ✅ Yes | Free |
| OS Terrain 50 | Elevation | OGL | ✅ Yes | Free |
| OpenTopoMap | Tiles | CC-BY-SA | ⚠️ Fair use | Free |
| Thunderforest Outdoors | Tiles | Proprietary + ODbL | ✅ Yes | $29-500+/month |
| Photon | Geocoding | ODbL | ⚠️ Fair use | Free |
| Nominatim (self-hosted) | Geocoding | ODbL | ✅ Yes | ~$50-100/month |

**Open Risks:**
- OSM PROW coverage completeness varies by region
- Free tile/geocoding services have no SLA; plan early migration
- Seasonal access data unavailable; requires partnerships/UGC
- Scotland Right to Roam mapping less explicit than E&W PROW

**Recommendation Summary:**
Optimal stack balances low initial cost (~$20-50/month MVP), data quality (good UK coverage), legal compliance (open licences or clear commercial terms), and technical feasibility (mature open-source tools).

### 2026-04-12: Round-trip routing support added

**Status:** IMPLEMENTED | **Owner:** Brand

#### Decision: Make RouteRequest.To optional; use GraphHopper round_trip algorithm when omitted

**Summary:**
RouteRequest now supports round-trip (circular) routing. The destination (To) field is optional; when omitted, GraphHopperClient uses GraphHopper's native `algorithm=round_trip` with a distance parameter instead of A→B routing.

**Implementation:**
- `RouteRequest.To` is nullable
- When To is null, GraphHopperClient calls GraphHopper with `algorithm=round_trip` and `round_trip.distance` (in metres)
- Distance unit conversion (km/mi → metres) happens in API layer (`Program.cs`)
- Single-point A→B routing remains unchanged

**Benefits:**
- Frontend can request routes by distance from a single start point
- Leverages GraphHopper's native round-trip support
- No breaking changes to existing A→B routing

**Validation:**
✅ Both projects build successfully  
✅ Commit: 6a4ec8a

---

### 2026-04-12: GraphHopper Dockerfile (Local Build)

**Status:** IMPLEMENTED | **Owner:** Brand

#### Decision: Build GraphHopper container image locally using Dockerfile in AppHost

**Problem:**
GraphHopper does not publish an official Docker image. Reference `graphhopper/graphhopper:latest` returns 404 on Docker Hub and 403 on GHCR. The previous `AddContainer(...)` call failed at runtime.

**Solution:**
Build a minimal GraphHopper container locally using `AddDockerfile` in `AppHost.cs`, allowing Aspire to handle the build automatically during `aspire run`.

**Implementation:**
- **`infra/graphhopper/Dockerfile`:**
  - Base: `eclipse-temurin:21-jre` (Java 21 JRE; GraphHopper 11.x requires Java 17+)
  - Downloads GraphHopper 11.0 web JAR from Maven Central at build time
  - ENTRYPOINT uses `sh -c` wrapper for `$JAVA_OPTS` expansion at runtime
  - CMD defaults to `--config /data/config.yml`
  - No USER instruction (avoids Podman namespace conflicts)

- **`src/Waymarked.AppHost/AppHost.cs`:**
  - Replaced `AddContainer(...)` with `AddDockerfile("graphhopper", "../../infra/graphhopper")`
  - Same bind mounts and environment configuration as before

- **`infra/graphhopper/docker-compose.yml`:**
  - Updated to use `build: .` instead of pre-built image
  - Config mount path aligned: `./config.yml:/data/config.yml:ro`

**Trade-offs:**
- First `aspire run` downloads JAR (~80MB) and builds image; subsequent runs use cache
- Requires Maven Central internet access in CI/CD environments
- Version pinned (11.0) for reproducibility

**Rationale:**
GraphHopper distributes via Maven Central JAR only; this is the supported self-hosted deployment path.

---

### 2026-04-12: Frontend Architecture — Zero-Build Web App with YARP Proxy

**Status:** IMPLEMENTED | **Owner:** Mouth (Frontend Dev)

#### Decision: Plain HTML/CSS/JS with YARP reverse proxy; no build step

**Problem:**
Building initial Waymarked.Web frontend to display GraphHopper-backed routes. Choose between SPA framework (React/Next.js) with build pipeline or plain HTML/CSS/JS.

**Decision:**
**Plain HTML/CSS/JS with no build step.**

**Rationale:**
- **Simplicity:** No npm, webpack, or build config overhead
- **Speed:** Instant iteration — refresh browser to see changes
- **Clarity:** Ideal for prototyping; can upgrade later if needed
- **Mobile-first:** Responsive CSS designed manually for better control

**Technical Stack:**
- **Leaflet 1.9.4** (CDN) — battle-tested, OSM-friendly
- **OpenTopoMap tiles** — topographic layer ideal for UK walking routes
- **YARP 2.2.0** reverse proxy — avoids CORS by proxying `/api/{**catch-all}` to `waymarked-api`
- **Aspire service discovery** — resolves backend URL automatically

**UI/UX:**
- Dark header, light sidebar, full-height map
- Mobile-responsive (sidebar stacks above map on narrow screens)
- Lat/lon inputs for start/end points
- Optional end point — defaults to start for round trips
- Stats panel: distance (km/mi), time, instruction count

**Files Created:**
- `src/Waymarked.Web/Waymarked.Web.csproj`
- `src/Waymarked.Web/Program.cs`
- `src/Waymarked.Web/appsettings.json`
- `src/Waymarked.Web/wwwroot/index.html`

**Updated:**
- `src/Waymarked.AppHost/AppHost.cs` — added web project reference
- `src/Waymarked.AppHost/Waymarked.AppHost.csproj` — added ProjectReference
- `src/Waymarked.sln` — added Waymarked.Web project

**Trade-offs:**
- ⚠️ No TypeScript safety
- ⚠️ Manual DOM manipulation in JS
- ⚠️ Will need refactor if app grows complex

**Migration Path:**
If scaling is needed:
1. Keep YARP proxy pattern (works with any frontend)
2. Migrate to Vite + React (or Next.js) while keeping same API contract
3. YARP config stays identical — just swap wwwroot content

---

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
