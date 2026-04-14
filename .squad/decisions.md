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

### 2026-04-12: WaymarkedRouteResponse introduced

**Status:** IMPLEMENTED | **Owner:** Brand (Backend Dev)

#### Decision: Added WaymarkedRouteResponse wrapper type with derived fields and distance validation bounds

**What:** Added WaymarkedRouteResponse wrapper type in Waymarked.Routing. POST /api/routes now returns derived fields (DistanceKm, DistanceMiles, DurationFormatted, IsRoundTrip) so the frontend doesn't compute them. Distance validation bounds set: 0.5 km min, 100 km max.

**Why:** Agreed with Mouth (frontend) and Data (GIS) to clean up frontend responsibilities and add sensible API-level validation.

**Implementation:**
- WaymarkedRouteResponse encapsulates route data with computed fields
- Distance bounds: 0.5 km–100 km (enforced at API layer)
- Frontend receives pre-formatted durations and distance conversions
- Reduces frontend logic complexity

**Validation:**
✅ Build successful  
✅ Commit: 9a63ea4

---

### 2026-04-12: Test Infrastructure Established

**Status:** IMPLEMENTED | **Owner:** Chunk (Tester)

#### Decision: xUnit test projects with lightweight HttpClient mocking

**What:** xUnit test projects created for Waymarked.Routing and Waymarked.Api. Tests cover routing logic, distance conversions, query string construction, and API endpoint validation.

**Key Patterns:**
- HttpClient mocked via custom DelegatingHandler (CapturingHandler) — simpler than NSubstitute for this use case
- WebApplicationFactory with StubGraphHopperHandler for API integration tests
- Avoided heavy mocking frameworks where possible; DelegatingHandler approach is lightweight and predictable
- Added `public partial class Program {}` to Program.cs for WebApplicationFactory type reference

**Test Coverage:**
- Waymarked.Routing.Tests: 27 tests (RouteRequest model, GraphHopperClient query strings, distance conversions)
- Waymarked.Api.Tests: 6 integration smoke tests (endpoint validation, happy/sad paths)
- **Total: 33 tests passing**

**Why:** Sets baseline for regression testing as features evolve. Tests cover A→B vs round-trip routing, unit conversions, query string construction, and error handling.

**Status:**
✅ All tests passing  
✅ Infrastructure worked first attempt

---

### 2026-04-12: Adopt NuGet Central Package Management

**Status:** IMPLEMENTED | **Owner:** Mikey (at Josh Hills request)

#### Decision: Migrated to CPM via Directory.Packages.props

**What:** All projects migrated to Central Package Management (CPM) via `src/Directory.Packages.props`. All package versions live in one place. Also added `src/Directory.Build.props` for shared Nullable/ImplicitUsings/TreatWarningsAsErrors.

**Why:** Single source of truth for dependency versions; easier upgrades; prevents version drift between projects.

**Packages Updated:**
- Yarp.ReverseProxy: 2.2.0 → 2.3.0
- coverlet.collector: 6.0.4 → 8.0.1
- Microsoft.NET.Test.Sdk: 17.14.1 → 18.4.0
- xunit.runner.visualstudio: 3.1.4 → 3.1.5
- Added Microsoft.Extensions.ServiceDiscovery.Yarp: 10.4.0 (implicit dependency from YARP + Aspire integration)

**Benefits:**
- Centralized version management
- Reduced duplicated version declarations
- Simplified dependency upgrades

---

### 2026-04-13: Route Polyline Colour — Magenta

**Status:** IMPLEMENTED | **Owner:** Mouth (Frontend Dev)

#### Decision: Route polyline fill colour set to magenta (#E0007A) with dark outline

**Problem:**
Initial implementation used vivid orange (#FF6B00) to improve contrast against water features on OpenTopoMap tiles. However, orange conflicts with A roads and motorways, which render in orange/red tones on OpenTopoMap. Users cannot reliably distinguish the route line from road features.

**Solution:**
Refined colour to magenta (#E0007A), which sits in the hue gap between all conflicting tile colours:
- Blue/Cyan (rivers, lakes, streams) — ✅ no conflict
- Orange/Red/Yellow (motorways, A roads, B roads) — ✅ no conflict
- Green (woodland, parks, fields) — ✅ no conflict
- Magenta/Hot pink (unused by map tiles) — ✅ clear gap

**Implementation:**
- Stacked polyline approach retained: dark outline (#1a0800, weight 7, opacity 0.55) + coloured fill
- File: `src/Waymarked.Web/wwwroot/js/route.js`
- Fill polyline colour changed to magenta only; outline pattern unchanged

**Rationale:**
Magenta reads clearly at all zoom levels. Vivid purple (#7B00FF) considered but rejected for lower luminance contrast against light beige/brown terrain tiles. Orange alternative (#FF006E) rejected for same reason.

**History:**
- **Commit 2d42598:** Initial orange (#FF6B00) with dark outline — improved water contrast but conflicted with road rendering
- **Commit 24309ba:** Refined to magenta (#E0007A) — final colour resolves all conflicts

**Validation:**
✅ Build passed  
✅ Both commits completed and deployed

---

### 2026-04-13: Add "Use My Location" Geolocation Feature

**Status:** IMPLEMENTED | **Owner:** Mouth (Frontend Dev)

#### Decision: Added "Use my location" button (crosshair icon) next to Start Point input for mobile UX

**Problem:**
Users on mobile had to manually type or tap a place name to set their start point. On a trail, this is awkward — you're already standing where you want to begin.

**Solution:**
Small `44×44px` icon button placed inline with the start point search field. Clicking it:
1. Shows a loading state (spinning crosshair animation)
2. Calls `navigator.geolocation.getCurrentPosition()`
3. On success: reverse-geocodes the coordinate via Nominatim, calls `setStartPoint()` with the result, pans the map to the location at zoom 14, and places a distinct "you are here" pulsing blue dot marker
4. On error: shows a friendly inline error message (e.g., "Location unavailable — please enable location access")
5. Hides itself entirely on browsers without geolocation support

**Key Files:**
- `src/Waymarked.Web/wwwroot/index.html` — Added `.search-with-geoloc` wrapper, `#geolocBtn`, `#geolocError`
- `src/Waymarked.Web/wwwroot/js/geolocation.js` — New file; geolocation logic
- `src/Waymarked.Web/wwwroot/css/app.css` — `.search-with-geoloc`, `.geoloc-btn`, `.geoloc-error`, `.yah-outer/.yah-inner`, dark mode overrides

**Design Decisions:**
- **Layout:** Flex row wrapping the existing `.search-wrapper` — no disruption to the existing form structure. Dropdown remains anchored to the input via `search-wrapper`'s `position: relative`.
- **Touch target:** Button is exactly `44×44px` — meets Apple/Google mobile HIG minimum tap target size.
- **"You are here" marker:** Blue pulsing dot (`#007AFF`) — deliberately distinct from route markers (green S, red E). Persists independently of the route start marker so users can see both their GPS location and chosen start point if they diverge.
- **Dark mode:** Button inherits `color: #ffffff` from the existing `[data-theme="dark"] button` rule. Hover state explicitly overridden to `color: #ffffff` to avoid specificity issues with pseudo-states.
- **Error UX:** Inline `<p role="alert" aria-live="polite">` element — announced by screen readers, visible below the search row, dark mode aware.
- **Geolocation options:** `timeout: 10000ms`, `maximumAge: 60000ms` — allows cached positions up to 1 minute old (fast on repeat taps), fails cleanly after 10 seconds.

**Dark Mode Cascade Note:**
The global `[data-theme="dark"] button { color: #ffffff }` rule has specificity `(0,1,0,1)`, which beats `.geoloc-btn { color: var(--clr-earth) }` at `(0,0,1,0)`. This means in dark mode, the icon is white on the dark button background — correct. The only override needed is `.geoloc-btn:hover` (specificity `(0,0,2,0)` — beats the global rule), so the hover state is explicitly pinned to `color: #ffffff` in dark mode.

**Script Load Order:**
`geolocation.js` loads after `geocoder.js` (uses `reverseGeocode`, `coordLabel`, `setStartPoint`) and `map.js` (uses `map` global). Load order: `map.js → geocoder.js → geolocation.js → markers.js → ...`

**Validation:**
✅ Build passed  
✅ Commit: e5113c5

---

### 2026-04-13: Active Backlog (user-confirmed)

**By:** Josh Hills (via Copilot)
**What:** Confirmed active backlog — items 4–7 from the previously presented list removed (round-trip accuracy, Thunderforest tiles, GraphHopper health check, integration tests). Self-hosted geocoding retained.
**Why:** User direction — keeping focus on user-facing features and production-readiness items only.

---

## Active Backlog

| # | Item | Owner | Notes |
|---|------|-------|-------|
| 1 | **User login / accounts** | Brand + Mikey | Auth system — needed before save or share can be built |
| 2 | **Route saving** | Brand + Mouth | Save routes to a user account; depends on auth (#1) |
| 3 | **Route sharing** | Brand + Mouth | Shareable links or social sharing of saved routes; depends on save (#2) |
| 4 | **Self-hosted geocoding** | Data / Brand | Replace public Photon instance with self-hosted for production SLA |

Items 1 → 2 → 3 are a dependency chain. Auth must land before save, save before share.

---

### 2026-04-14: Code Quality Review — Full Codebase

**Status:** COMPLETED | **Owner:** Mikey (Lead) | **Reviewer:** claude-opus-4.6

#### Finding: updateFieldStates() undefined — Critical bug in frontend

**Problem:**
`geocoder.js` calls `updateFieldStates()` in 7 places (lines 47, 50, 57, 82, 85, 93, 156) but this function is never defined anywhere in the codebase. This causes `ReferenceError: updateFieldStates is not defined` at runtime whenever:
- A start/end point is set or cleared
- A marker is dragged
- A search input is edited

**Resolution:**
Mouth (Frontend) removed all 7 dead calls to `updateFieldStates()` in geocoder.js. The function was deleted during the Route Type Toggle rewrite but the callers were never cleaned up. These were silent ReferenceErrors in event handlers — now eliminated.
- **Commit:** af77b47 (comment cleanup + bug fix)
- **Status:** RESOLVED ✅

---

### 2026-04-14: Export Endpoint DRY Violation — Backend Refactoring Opportunity

**Status:** IDENTIFIED (not yet implemented) | **Owner:** Brand (Backend Dev) | **Priority:** Medium

#### Finding: Duplicate validate→build→execute pipeline across export endpoints

**Problem:**
Three export endpoints (`/api/routes/export/gpx`, `/api/routes/export/kml`, `/api/routes/export/geojson`) in `Program.cs:114-176` repeat the exact same validation → build → execute pipeline as `/api/routes`. Only the final serialisation differs.

**Solution Recommendation:**
Extract a single `GetRouteOrError()` helper method that returns the `WaymarkedRouteResponse` or an `IResult` error. Each endpoint would then just call that + format-specific serialization.

**Rationale:**
- Reduces maintenance burden — validation or routing logic changes only need to be updated in one place
- Prevents bugs from inconsistent validation across endpoints
- Not urgent but will cause maintenance issues when validation logic evolves

**Status:** Deferred; helpers at lines 200-299 already partially mitigate this.

---

### 2026-04-14: Auth Hardening — Lockout, Token Lifespan, Rate Limiting

**Status:** IMPLEMENTED | **Owner:** Brand (Backend Dev) | **Date:** 2026-04-15

#### Decision: Pre-production security hardening for auth system

**Implemented Changes:**

**1. Account Lockout on Failed Login Attempts**
- Configuration: 5 failed attempts → 15 minute lockout
- Settings:
  - `MaxFailedAccessAttempts = 5`
  - `DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15)`
  - `AllowedForNewUsers = true`
  - `lockoutOnFailure: true` in PasswordSignInAsync

**2. Password Reset Token Lifespan**
- Configuration: 24-hour token expiry
- Location: `Program.cs` (Configure<DataProtectionTokenProviderOptions>)
- Setting: `TokenLifespan = TimeSpan.FromHours(24)`

**3. Rate Limiting on Forgot-Password Endpoint**
- Policy: Fixed-window rate limiter (3 requests per 15 minutes)
- Behavior: Returns HTTP 429 (Too Many Requests) when limit exceeded
- Disabled in Test environment to prevent cross-test interference

**4. Integration Tests for Password Reset Flow**
- 7 new integration tests added
  - Forgot-password (4 tests): known email, unknown email, empty email, null body — all return 200 for anti-enumeration
  - Reset-password (3 tests): invalid token, missing fields, unknown email — all return 400
- Test Infrastructure: FakeEmailSender stubs SMTP in tests

**Files Modified:**
- `src/Waymarked.Api/Program.cs` — lockout, token, rate limiting configuration
- `src/Waymarked.Api/AuthEndpoints.cs` — lockoutOnFailure: true, RequireRateLimiting
- `src/Waymarked.Api.Tests/AuthEndpointTests.cs` — 7 new tests
- `src/Waymarked.Api.Tests/AuthWebApplicationFactory.cs` — FakeEmailSender

**Test Results:**
- Total: 45 tests (38 pre-existing + 7 new)
- Passing: 42 (35 pre-existing + 7 new)
- Failing: 3 (pre-existing, unrelated)
- All 7 new tests passing ✅

**Trade-offs:**
- **Lockout:** 5 attempts, 15 minutes balances security vs. UX
- **Rate Limiting:** 3 requests per 15 minutes allows 1-2 legitimate retries, blocks enumeration
- **Token Lifespan:** 24 hours — standard industry practice

---

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
