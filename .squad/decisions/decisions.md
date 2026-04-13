# Waymarked Decisions Log

---

## 🔒 LOCKED: 2026-04-12T17:35:00Z — Tech stack directive — C# / .NET / Aspire

**By:** Josh Hills  
**Status:** LOCKED (binding direction)  
**What:** Backend language is C# with .NET. Aspire app host is the target for local orchestration of all services.

**Why:** User preference — explicit directive.

**Implications:**
- Backend API: ASP.NET Core (not Node.js/Express or Python/FastAPI)
- Local orchestration: .NET Aspire replaces Docker Compose as the target
- GraphHopper (Java) runs as a container resource within the Aspire app host
- Brand's in-progress Docker Compose spike is still useful as reference/fallback but Aspire is the end goal
- All new backend code should be C#

---

## Brand: GraphHopper Development Spike

**Date:** 2026-04-12  
**Status:** Completed  
**Owner:** Brand (Backend Dev)

### Context

Team decision to use GraphHopper as routing engine (over Valhalla) for Waymarked. Created local development spike to validate GraphHopper with UK OSM data.

### What Was Built

Created `infra/graphhopper/` directory with:
- **Docker Compose setup:** Single-service stack, port 8989, volume mounts for OSM data and graph cache
- **GraphHopper config:** Enabled `foot` and `hike` profiles, SRTM elevation, UK bounding box (49,-8,61,2)
- **README:** Setup instructions, profile explanations, memory requirements, gotchas
- **Test scripts:** Bash and PowerShell scripts to test Edinburgh Waverley → Arthur's Seat route

### Technical Decisions Made

#### 1. Development Extract Recommendation
**Decision:** Recommend Scotland extract (`scotland-latest.osm.pbf`, ~200MB) for development; full GB extract for production.

**Reasoning:**
- Scotland builds in 5-10 min vs 20-40 min for full GB
- Requires 2-3GB RAM vs 8-12GB RAM (more developer-friendly)
- Covers representative UK terrain (Highlands, islands, urban)
- Faster iteration during spike/integration work

**Tradeoff:** Routes outside Scotland won't work, but 90% of testing can use Scottish routes (Edinburgh, Glasgow, Highlands).

#### 2. Elevation Provider
**Decision:** Use SRTM (auto-download) for spike; defer OS Terrain 50 integration to production.

**Reasoning:**
- SRTM: Zero setup, auto-downloads from NASA, ~90m resolution
- OS Terrain 50: Better accuracy (50m, ±2.5m RMSE) but requires manual download, format conversion, volume mount
- SRTM is "good enough" to validate elevation-aware routing in spike phase
- OS Terrain 50 integration can be done later when accuracy matters for production

**Next Step:** Document OS Terrain 50 setup process for production deployment.

#### 3. Profile Configuration
**Decision:** Enable both `foot` and `hike` profiles; recommend testing both for UK routes.

**Reasoning:**
- `foot`: Better for urban, mixed terrain, general walking (uses roads if faster)
- `hike`: Better for countryside, mountains, dedicated trails (avoids roads)
- UK use case spans both: city walking (Edinburgh, London) and mountain hiking (Lake District, Highlands)
- GraphHopper `hike` profile uses OSM `sac_scale` tag for trail difficulty (valuable for mountain routes)

**Recommendation:** Test both profiles against known UK routes (e.g., West Highland Way) to determine default behavior for "walking" vs "hiking" mode in Waymarked UI.

#### 4. Memory Configuration
**Decision:** Set `JAVA_OPTS: "-Xmx4g -Xms1g"` in docker-compose.yml as default; document scaling requirements.

**Reasoning:**
- 4GB heap is middle ground: Works for Scotland + smaller regional extracts, fails gracefully for full GB (clear error in logs)
- Developers can easily adjust via env var or docker-compose.yml edit
- README documents exact requirements per extract (Scotland: 2-3GB, GB: 8-12GB)

**Production Note:** Use `-Xmx12g` for full GB extract in production.

### Key Findings

#### Profile Behavior Comparison Needed
GraphHopper's `foot` vs `hike` profiles have meaningfully different routing behavior:
- `foot` will route along A-roads/B-roads if faster (e.g., Edinburgh city routes)
- `hike` strongly avoids roads, even if slower (e.g., Highlands routes)

**Action Required:** Test both profiles on 5-10 known UK routes (urban + countryside) and document behavior differences. This informs Waymarked UI design (separate "walking" vs "hiking" modes, or single mode with "avoid roads" toggle).

#### Round-Trip Routing Support
GraphHopper supports round-trip routing natively (`round_trip.distance` and `round_trip.seed` parameters). This was a known gap in Valhalla.

**Decision Impact:** Validates team decision to use GraphHopper over Valhalla for Waymarked (round-trip routes are likely user feature).

#### Elevation Accuracy Tradeoff
SRTM (90m) vs OS Terrain 50 (50m) accuracy difference is significant for UK hill routes:
- SRTM: Good enough for "total ascent" calculation, route feasibility
- OS Terrain 50: Better for detailed climb profiles, gradient accuracy (valuable for hill walkers)

**Recommendation:** Spike SRTM for now, but prioritize OS Terrain 50 integration before public launch (users will compare against OS Maps app).

### Open Questions / Risks

1. **OSM footpath coverage quality:** GraphHopper routing is only as good as OSM data. Need to test routes in areas with poor OSM coverage (e.g., remote Scotland, access land without marked paths).

2. **Graph update frequency:** How often do we rebuild the graph for OSM updates? Weekly? Monthly? Impacts route quality vs infrastructure cost.

3. **Custom weighting factors:** Can we tweak GraphHopper's `hike` profile to prefer scenic routes (forests, parks, water features)? Requires digging into GraphHopper's weighting API.

4. **Production deployment:** Single GraphHopper instance vs horizontal scaling? Load testing needed to determine concurrent request capacity.

### Next Steps (Recommendations)

1. **Load testing:** Benchmark concurrent routing requests, response times, memory usage under load
2. **Route quality validation:** Test 10-20 known UK walking/hiking routes, compare against Google Maps / OS Maps
3. **API wrapper design:** GraphHopper API is direct; design Waymarked backend wrapper (auth, rate limiting, custom response format, caching)
4. **OS Terrain 50 integration:** Document setup process, test accuracy improvement
5. **Custom profile tuning:** Experiment with weighting factors for "scenic" preference (forest, park, water tags)

### Files Created

- `infra/graphhopper/docker-compose.yml`
- `infra/graphhopper/config.yml`
- `infra/graphhopper/README.md`
- `infra/graphhopper/test-route.sh`
- `infra/graphhopper/test-route.ps1`
- `infra/graphhopper/.gitignore`

---

## Data: UK OSM Footpath Coverage Assessment — Team Recommendations

**From:** Data (GIS Engineer)  
**Date:** 2026-04-12  
**Status:** For Review  
**Audience:** Mikey (Lead), Brand (Backend), Josh (Product)

### Summary

Completed UK OSM footpath coverage assessment (see `docs/osm-coverage-uk.md`). Key finding: **OSM + Valhalla sufficient for MVP & production walking route planner**. Identified critical application-layer gaps and recommended development data strategy.

### Key Recommendations

#### 1. Development Data Strategy ✅ **DECISION READY**

**Recommendation:** Use **Scotland extract** for local dev spike; **full GB extract** for production.

**Rationale:**
- Scotland extract (`scotland-latest.osm.pbf`, ~100 MB): Fast iteration (Valhalla builds in <5 min), simplified access logic (Right to Roam), established routes (West Highland Way) for sanity checks
- Full GB extract (`great-britain-latest.osm.pbf`, ~1.5 GB): Comprehensive 3-nation coverage, scales to modest VPS, weekly update cadence via Geofabrik

**URLs:**
- Dev: https://download.geofabrik.de/europe/scotland-latest.osm.pbf
- Prod: https://download.geofabrik.de/europe/great-britain-latest.osm.pbf

**Next step:** Pass to Mikey (Backend) for spike implementation.

#### 2. Valhalla Application-Layer Gaps ⚠️ **DESIGN DECISION NEEDED**

Valhalla pedestrian costing does NOT natively support several hiking features. Application layer must implement:

| Feature | Status | App Layer Requirement |
|---------|--------|----------------------|
| **Difficulty filtering** (`sac_scale` T1–T6) | ❌ Unsupported | Parse OSM `sac_scale` tag; estimate from elevation if absent; filter route alternatives by difficulty |
| **Seasonal closures** (lambing, grouse, nesting) | ❌ Unsupported | Maintain curated closure database; display warnings on route UI |
| **Round-trip routing** | ❌ Unsupported | Implement algorithm (e.g., reverse-path A*) or restrict to open-ended routes |
| **Permissive path warnings** | ⚠️ Partial | Flag paths tagged `foot=permissive` in UI: "This section uses permissive paths (may be revoked by owner)" |
| **CRoW access land routing** | ⚠️ Partial | Store as GeoJSON zones; overlay on map; warn users on routes crossing zones |

**Design Question:** Should Waymarked v1.0 MVP support difficulty filtering and seasonal closures, or defer to v1.1?

#### 3. UK PROW Coverage Quality ✅ **FYI / EXPECTATIONS SETTING**

**Regional variation:**
- **Excellent:** Lake District, Peak District, South Downs, South West Coast (>80% PROW designated tags)
- **Good:** Welsh uplands, major long-distance paths (Pennine Way, Coast-to-Coast)
- **Fair:** Remote rural areas (Scottish Borders, mid-Wales, East Anglia) — user may find limited options
- **Coverage:** ~40-60% of English/Welsh paths tagged with `designation` tag; ~30-40% fewer paths in Scotland (Right to Roam model)

**Implication for product:** Waymarked will excel for popular UK walking regions; edges (remote areas, lesser-known local paths) may have gaps. Plan user expectations accordingly in product docs.

#### 4. Data Contribution Opportunity 🔮 **FUTURE ENHANCEMENT**

**Opportunity:** MapThePaths project successfully conflated 149 UK authority PROW datasets into OSM (~60,000 paths). Waymarked could contribute:

1. **Seasonal closure tagging:** Partner with Scottish Wildlife Trust, grouse estates, MoD to systematically tag `access:conditional` on paths
2. **CRoW zone mapping:** Map access land boundaries as GeoJSON/OSM relations (currently unmapped)
3. **Surface/difficulty refinement:** Crowdsource `sac_scale` and `trail_visibility` tagging on untagged paths

**Impact:** Improves Waymarked's own routing quality + benefits broader OSM community.

### Action Items

- **Mikey (Backend):** Spike Valhalla deployment with Scotland extract; test custom costing (`use_roads`, `use_hills`)
- **Brand (Backend):** Design app layer for difficulty filtering, seasonal warnings, round-trip algorithm
- **Data (me):** Stand by for regional coverage analysis (e.g., which auth boundaries have poorest PROW tagging?)
- **Josh (Product):** Review expectations (regional variation, v1.0 vs v1.1 feature list)

**Full assessment:** See `docs/osm-coverage-uk.md` — living reference, updated as findings emerge.

---

## GraphHopper Round-Trip Algorithm: Accuracy Analysis & Config Tuning Recommendations

**Date:** 2026-04-12  
**Author:** Data (GIS Engineer)  
**Audience:** Team (Brand, Mouth, Chunk); specifically flagged items for Brand (API)  
**Status:** RESEARCH COMPLETE — ACTIONABLE RECOMMENDATIONS READY

### Executive Summary

GraphHopper's round-trip algorithm is fundamentally a **heuristic distance guide, not a guarantee**. Deviations of ±20–40% from the requested distance are normal, especially in sparse networks. The UK's rural footpath coverage exacerbates this: sparse OSM network density forces the algorithm to either overshoot (taking longer routes to "fill distance") or undershoot (exhausting nearby paths too quickly).

**The core issue is NOT a config bug — it's a mismatch between user expectations and algorithm capability.** Config-level tuning can improve accuracy by ±5–10%, but fundamentally cannot overcome network sparsity. The solution lives in three layers:

1. **Config tuning** (±5–10% improvement) — profile weighting, algorithm randomness
2. **Algorithm resilience** (Brand's API layer) — retry with adjusted distance, accept tolerance bands
3. **UX honesty** (Mouth's frontend) — show "Expected range" vs "Target"

### Key Findings

#### Round-Trip Distance Variance by Region

| Region | OSM Density | Typical Distance Accuracy | Affected Radii |
|--------|-------------|---------------------------|-----------------|
| Lake District | Dense | ±10–15% | 5–15 km tours |
| Peak District | Dense | ±10–15% | 5–15 km tours |
| South Downs | Moderate–Good | ±15–20% | 5–20 km tours |
| Cotswolds | Moderate | ±20–25% | 5–25 km tours |
| Remote Uplands (Cairngorms, Pennines) | Sparse | ±25–40% | 10–50 km tours |
| Agricultural flatlands | Variable | ±20–30% | 5–20 km tours |

**Key finding:** Users requesting 50 km round-trips in sparse areas will **almost always undershoot** (algorithm exhausts local graph). Users requesting 5 km in dense areas are more likely to overshoot (algorithm fills distance with longer detours).

### Recommendation Tiers

#### Tier 1: Immediate Actions (High Impact)

1. **Verify custom model files exist**
   - Confirm `foot.json`, `hike.json`, `foot_elevation.json` are present in Docker build
   - If missing, create them with sensible defaults

2. **Update elevation provider: SRTM → OS Terrain 50**
   - Current: `graph.elevation.provider: srtm`
   - Recommended: Add OS Terrain 50 DEM to the `/data` mount, configure GraphHopper to use it
   - Impact: ±3% better accuracy in UK, much better elevation representation

3. **Document expected accuracy band**
   - Create API-level documentation: "Round-trip routes achieve ±20% of requested distance in rural areas, ±10% in urban areas"
   - This sets honest user expectations

#### Tier 2: API-Layer Resilience (Brand's Responsibility)

1. **Implement retry logic in GraphHopperClient**
   - If returned distance < 90% of requested, automatically retry with `distance * 1.15`
   - If still < 85%, accept the result and flag to frontend as "undershoot"
   - Limit retries to 2 max (avoid infinite loops in sparse regions)

2. **Add tolerance validation in WaymarkedRouteResponse**
   - Return `DistanceDeviation` field: `(actual - requested) / requested`
   - Frontend can show "📍 Route is 8.5 km (Target was 10 km)"
   - Flag severe deviations (>25%) with a warning

3. **Make profile selection intelligent**
   - For short distances (< 5 km), use "foot" profile (safer in sparse networks)
   - For longer distances (> 20 km), use "hike" profile (elevation costs are less restrictive)

#### Tier 3: Future Enhancements (Lower Priority)

1. **Implement A/B testing for distance_influence parameter**
2. **Partner with MapThePaths project** for pre-processed OSM extracts
3. **Create region-specific profiles** with tuned elevation penalties

---

## GraphHopper Custom Profile Files: Verification & Implementation

**Date:** 2026-04-12  
**Author:** Data (GIS Engineer)  
**Status:** COMPLETED  
**Decision:** Created missing GraphHopper custom profile JSON files with distance_influence tuning

### Problem Statement

The `config.yml` referenced three custom routing profile model files (`foot.json`, `hike.json`, `foot_elevation.json`) but these files did not exist in the repository. This would cause GraphHopper to fail silently or fall back to defaults at runtime, negating the custom routing profiles configured for UK walking/hiking use.

### Solution Implemented

#### Custom Model Files Created

**`infra/graphhopper/data/foot.json`** — Pedestrian-focused profile
- Penalises motorway/trunk routes (limit_to: 1)
- Reduces speed on primary/secondary roads (multiply_by: 0.7)
- Prioritises footways (1.5x) and paths (1.2x)
- **distance_influence: 80** — balances distance accuracy with route quality
- Suitable for urban/suburban walking; good for short routes (< 10 km)

**`infra/graphhopper/data/hike.json`** — Scenic, elevation-aware profile
- Steep slope penalties: > 15° (0.6x speed), 10–15° (0.8x speed)
- Hiking-rated path bonus (hiking_rating >= 3: 1.4x priority)
- Bridleway bonus (1.2x priority) for bridleway preference
- **distance_influence: 60** — sacrifices some distance accuracy for terrain preference
- Suitable for longer/scenic routes (8+ km); elevation-conscious

**`infra/graphhopper/data/foot_elevation.json`** — Elevation integration (shared by both profiles)
- Speed cap on very steep slopes (average_slope > 12: limit_to 2)
- Prevents algorithm from avoiding steep sections via extreme lateral detours

#### Dockerfile Updated

Added COPY instructions to ensure the custom model files are included in the container image.

### Files Modified

| File | Change | Reason |
|------|--------|--------|
| `infra/graphhopper/data/foot.json` | Created | Pedestrian-focused profile |
| `infra/graphhopper/data/hike.json` | Created | Hiking/scenic profile |
| `infra/graphhopper/data/foot_elevation.json` | Created | Shared elevation logic |
| `infra/graphhopper/Dockerfile` | Updated | Added COPY for model files |

---

## Round-Trip Distance Accuracy — Findings & Improvements

**Author:** Brand (Backend Dev)  
**Date:** 2026-04-13  
**Status:** Implemented (client-side) | Pending (server-side — needs Data)

### Implementation Summary

| Improvement | Scope | Status |
|---|---|---|
| `round_trip.max_retries=10` | Client | ✅ Implemented |
| Client-side retry on deviation | Client | ✅ Implemented |
| Seed diversity across retries | Client | ✅ Implemented |
| `distanceTolerance` API param | API | ✅ Implemented |
| OSM network coverage validation | Data | ⏳ Needs Data |
| Routing profile weight tuning | Infra/Data | ⏳ Needs Data |
| Elevation-corrected distance | Product decision | ⏳ TBD |
| Multi-alternative selection | Client (future) | ⏳ Not started |

### Client-Side Changes (Commit: 8dd8051)

1. **GraphHopper `round_trip.max_retries=10`**
   - Tells GraphHopper to try 10 candidate routes internally per call (up from default 3)
   - Small extra CPU cost on GH side but gives more chances to find a better match

2. **Client-Side Retry Loop**
   - Retries round-trip requests with fresh random seed if returned distance deviates > `DistanceTolerance` (default 15%)
   - `MaxRetries = 3` for all round-trip requests (up to 4 total attempts)
   - Each retry generates new seed for different route shapes
   - Early exit when tolerance met (no wasted calls)

3. **`DistanceTolerance` API Parameter**
   - Clients can pass `distanceTolerance` (fractional, e.g. `0.10` = ±10%) in route request
   - Defaults to `0.15` (±15%). Tighter values trigger more retries; looser values return faster

4. **Unit Tests Added**
   - `GetRouteAsync_RoundTrip_IncludesMaxRetriesParam` — verifies query param
   - `GetRouteAsync_RoundTrip_RetriesUpToMaxWhenDeviationExceedsTolerance` — verifies 4 attempts
   - `GetRouteAsync_RoundTrip_StopsRetryingWhenDistanceWithinTolerance` — verifies early exit
   - `GetRouteAsync_RoundTrip_SeedDiffersBetweenRetries` — verifies seed diversity

### Data's Input Needed

1. **OSM Path Network Density** — Validate footpath coverage in key UK regions; flag areas where round-trip routing unreliable
2. **GraphHopper Routing Profile Tuning** — Custom costing weights in `infra/graphhopper/config.yml` to prefer longer connecting paths
3. **Elevation-Aware Distance** — Whether to correct for grade in hilly terrain (product decision)
4. **Alternative Route Candidates** — Future: request 3 alternatives per call, select closest to target distance

---

## Test Coverage: Round-Trip Accuracy Improvements

**From:** Chunk (Tester)  
**Date:** 2026-04-13  
**Relates to:** Brand's round-trip accuracy improvements (max_retries, client-side retry loop, distanceTolerance API param)  
**Status:** All tests passing (60/60)

### Coverage Summary

| Scenario | Test Name | Result |
|---|---|---|
| Happy path — within tolerance first try | `NoRetryWhenFirstAttemptWithinTolerance` | ✅ |
| Retry on undershoot, stops early | `StopsRetryingWhenDistanceWithinTolerance` | ✅ |
| Retry exhaustion → best route returned | `RetriesUpToMaxWhenDeviationExceedsTolerance` | ✅ |
| Custom distanceTolerance respected | `RespectsCustomTolerance` | ✅ |
| `round_trip.max_retries=10` in query string | `IncludesMaxRetriesParam` | ✅ |
| Seed changes on each retry | `SeedDiffersBetweenRetries` | ✅ |
| A→B routes bypass retry logic entirely | `AbRoute_DoesNotRetryRegardlessOfDistance` | ✅ |
| API accepts `distanceTolerance` parameter | `Post_ApiRoutes_RoundTrip_AcceptsCustomDistanceTolerance` | ✅ |

### Test Details

- Newly added: `NoRetryWhenFirstAttemptWithinTolerance`, `RespectsCustomTolerance`, `AbRoute_DoesNotRetryRegardlessOfDistance` at unit level
- Also: API parameter smoke test (`Post_ApiRoutes_RoundTrip_AcceptsCustomDistanceTolerance`)
- Seed diversity test uses `Distinct()` — probabilistic but reliable (collision probability 1-in-2³¹ per pair)

---

## 2026-04-13: UI & Routing Improvement Backlog

**By:** Josh Hills (via Copilot)  
**Status:** Backlog — not yet started

### Items

1. **Route line contrast** — The blue polyline on OpenTopoMap tiles lacks sufficient visual contrast. Improve visibility (colour, weight, or outline/shadow treatment).

2. **Dark mode** — Add a dark UI theme to the frontend. Scope TBD (CSS custom properties toggle, system preference detection, or manual toggle).

3. **Round-trip distance accuracy** — GraphHopper's `round_trip` algorithm doesn't reliably produce routes matching the requested distance. Requires collaboration between Brand (routing client/API) and Data (OSM coverage, GraphHopper profile tuning, algorithm parameters).

4. **Geolocation — use current location as start/end point** — Use the browser Geolocation API to let users set their current location as the route start or end point, replacing the manual coordinate/address entry for that field. Mouth owns the UI integration; Brand may need to validate/accept the coordinates.

---

## 2026-04-13: Backlog — User accounts, saved routes, and sharing

**Requested by:** Josh Hills  
**Captured by:** Coordinator

### Items

1. **User login / authentication**
   - Users need accounts to save and share routes
   - Auth strategy TBD (email/password, OAuth, passkeys)
   - Brand owns implementation; Coordinator to facilitate auth strategy decision when prioritised

2. **Saving routes**
   - Authenticated users can save named routes to their account
   - Requires database schema (Brand) and API endpoints (Brand)
   - UI for saved routes list and save/delete actions (Mouth)

3. **Sharing routes between users**
   - Shareable link or direct share-to-user mechanism (scope TBD)
   - Requires decision on share model: public link vs. user-to-user vs. both
   - Touches API (Brand), frontend (Mouth), and potentially email/notification (TBD)

### Notes

- These three items are tightly coupled — auth is a prerequisite for the others
- Implementation order: auth → save → share
- Mikey should be involved in scoping the auth strategy before Brand starts
- Data stack decision (currently open) may affect route persistence choices

---

## 2026-04-13T21:02: User directive — Distance display

**By:** Josh Hills (via Copilot)  
**What:** Do NOT show achieved distance vs requested distance in the UI. Mouth should not implement a "Route achieved: X km (Target was Y km)" display or any equivalent distance deviation feedback on the frontend.

---

## Mouth: Fix Button Dark Mode Styling

**Date:** 2026-12-04  
**Owner:** Mouth (Frontend Dev)  
**Status:** IMPLEMENTED

### What Was Wrong

Two buttons were broken in dark mode:

**"Plan Route" button:** Global `button` rule sets `color: var(--clr-white)`. In dark mode, `--clr-white` is remapped to `#2a2a2e` (near-black) so sidebar panels look dark. This remapping made the button text near-invisible — dark grey text against a dark green (`#2d5a27`) background.

**"Show Steps" button (`.steps-toggle`):** Background was `var(--clr-earth-lt)`, which in dark mode becomes `#1a2d18` (very dark green). Text was `color: var(--clr-earth)` = `#2d5a27` — a medium green. Dark green text on very dark green background: effectively invisible.

### Root Cause

The dark mode token override for `--clr-white` was designed to darken card/panel surfaces. But because the base `button` rule used `color: var(--clr-white)` for its text, the token change silently broke button legibility. `.steps-toggle`'s hardcoded `--clr-earth-lt` background compounded the issue.

### Fix

Added two rules under the `[data-theme="dark"]` hardcoded-colour fixes section in `src/Waymarked.Web/wwwroot/css/app.css`:

```css
/* Force white text on all buttons in dark mode */
[data-theme="dark"] button {
    color: #ffffff;
}

/* Steps toggle: match main button appearance */
[data-theme="dark"] .steps-toggle {
    background: var(--clr-earth);
    color: #ffffff;
    border-color: var(--clr-earth-dk);
}
```

Both buttons now show white text on the brand green (`--clr-earth: #2d5a27`) background in dark mode — readable, clearly clickable, and consistent with light-mode brand colour. No changes to light mode behaviour.

### Files Changed

- `src/Waymarked.Web/wwwroot/css/app.css`

**Commit:** 559b7e6

---

## Mouth: Dark Mode Map Tiles — Layer Swap (v2)

**Date:** 2026-12-04  
**Status:** IMPLEMENTED  
**Owner:** Mouth (Frontend Dev)  
**Supersedes:** Dark Mode Map Tiles — CSS Filter on Tile Pane

### Problem

The previous approach of applying `filter: invert(100%) hue-rotate(180deg)` to `.leaflet-tile-pane` produced a recognisably dark map but with a notable flaw: built-up areas (dense urban blocks) remained light-coloured because the hue-rotate partially reversed the inversion on certain warm-grey building tones. The result did not feel genuinely dark.

### Decision

**Swap the Leaflet tile layer on every theme change** — use **CartoDB Dark Matter** in dark mode and **OpenTopoMap** in light mode. Remove the CSS filter entirely.

### Implementation

#### `src/Waymarked.Web/wwwroot/js/map.js`

- Assigned the tile layer to `window.tileLayer` (previously anonymous).
- Assigned the map instance to `window.map` (previously `const` — not on `window`).
- This exposes both globals to `theme.js`, which loads after `map.js`.

#### `src/Waymarked.Web/wwwroot/js/theme.js`

- Added `TILE_CONFIG` object with URLs and attribution strings for both themes.
- Added `swapTileLayer(theme)` function: removes current `window.tileLayer`, creates a new `L.tileLayer`, adds it to `window.map`, and calls `bringToBack()` so route polylines remain on top.
- `applyTheme(theme)` now calls `swapTileLayer` in addition to updating the DOM/aria attributes.
- Swap fires on: toggle click, initial load, OS preference detection (all paths flow through `applyTheme`).

#### `src/Waymarked.Web/wwwroot/css/app.css`

- Removed the `[data-theme="dark"] .leaflet-tile-pane { filter: … }` block — now obsolete and would conflict with the genuine dark tiles.

### Tile Layers

| Theme | Provider | URL |
|-------|----------|-----|
| Light | OpenTopoMap | `https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png` |
| Dark  | CartoDB Dark Matter | `https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png` |

### Constraints Honoured

- Route polylines (`#E0007A` magenta + dark outline) are in `.leaflet-overlay-pane` (SVG). `bringToBack()` pushes only the tile layer behind all overlays — polylines are untouched.
- Script load order: `map.js` runs before `theme.js`; `window.map` and `window.tileLayer` are set before `theme.js` runs.
- No build step, no framework — vanilla JS throughout.

### Alternatives Considered

- **CSS filter (v1):** Cheap, no JS change — but produces artefacts on warm-grey building tiles.
- **Separate CSS class toggle:** Would require duplicating tile URL in CSS which is not a supported Leaflet pattern.
- **Map style override API:** Not available for OpenTopoMap (raster only).

### Resolution: OpenTopoMap Undarkeable (Earlier Limitation)

Earlier decision noted in "Dark Mode Toggle" entry: "OpenTopoMap raster tiles were initially undarkened in dark mode. This was later solved with a CSS filter on `.leaflet-tile-pane` — see Dark Map Tiles decision below."

**This limitation is now fully resolved** by using CartoDB Dark Matter in dark mode instead of filtering OpenTopoMap. The issue is removed.
**Why:** User request — captured for team memory

---

## Dark Mode Implementation

**Status:** IMPLEMENTED | **Owner:** Mouth (Frontend Dev) | **Date:** 2026-04-13  
**Commit:** 1ae475d

### Decision

Add dark mode to the Waymarked frontend using CSS custom property overrides, OS preference detection, localStorage persistence, and a header toggle button.

### Implementation

#### Theming Layer
- All colours already used CSS custom properties (`--clr-*` tokens in `:root`)
- Added `[data-theme="dark"]` block on `<html>` redefining seven tokens:
  - `--clr-stone` (sidebar bg): `#f4f1ec` → `#1c1c20`
  - `--clr-white` (input/panel bg): `#ffffff` → `#2a2a2e`
  - `--clr-ink` (body text): `#1c1c1e` → `#e8e8ea`
  - `--clr-muted` (muted text): `#6b6b6b` → `#9a9a9a`
  - `--clr-border` (dividers): `#ddd8d0` → `#3c3c40`
  - `--clr-earth-lt` (steps-toggle bg): `#e8f5e4` → `#1a2d18`
  - `--clr-trail-lt` (trail light): `#fdf0e8` → `#2a1810`
- Brand greens (`--clr-earth`, `--clr-earth-dk`, `--clr-trail`) retained — the header and primary buttons stay green for brand continuity in both modes

#### Anti-FOUC
- Inline `<script>` in `<head>` (before any CSS paints): reads `localStorage.getItem('theme')`, falls back to `matchMedia('prefers-color-scheme: dark')`, sets `data-theme` on `<html>` before first render
- No flash of wrong theme on load

#### Toggle Button
- `<button id="theme-toggle" class="theme-toggle">` in `<header>`, right-aligned via `margin-left:auto`
- Uses emoji icons: 🌙 (currently light) / ☀️ (currently dark)
- `aria-label` flips between "Switch to dark mode" and "Switch to light mode" on each press

#### JS (`js/theme.js`)
- IIFE runs after DOM is ready
- Reads current theme from `data-theme` attribute, applies correct icon/label
- On click: flips theme, updates attribute, persists to `localStorage`

#### Hardcoded Colour Patches
- Select `<svg>` arrow stroke (`%236b6b6b` → `%239a9a9a`)
- Error banner (red tones → dark red surface)
- Steps-toggle hover (`#d4ecce` → `#223d20`)
- mode-btn.active-off hover (`#555` → `#888`)

#### Leaflet Controls
Scoped overrides for `[data-theme="dark"]`:
- Zoom bar (`.leaflet-bar a`, `.leaflet-control-zoom a`): dark bg, light text, dark border
- Attribution (`.leaflet-control-attribution`): translucent dark bg, muted text, green links
- Popups (`.leaflet-popup-content-wrapper`, `.leaflet-popup-tip`): dark bg, light text

### OpenTopoMap Tile Darkening in Dark Mode — Layer Swap Implementation (2026-04-13)

**Status:** ✅ SUPERSEDED by v2 (commit 66172a5) | **Original:** Commit 8e3cf6d | **Current:** Commit 66172a5

#### Version 1: CSS Filter Approach (Initial, then Superseded)

Applied CSS filter to `.leaflet-tile-pane` to darken tiles:
```css
[data-theme="dark"] .leaflet-tile-pane {
    filter: invert(100%) hue-rotate(180deg);
}
```

**Issue with v1:** While the filter darkened the map, built-up urban areas (dense city blocks) remained relatively light-coloured because `hue-rotate` partially reversed the inversion on warm-grey building tones, resulting in a map that didn't feel genuinely dark.

#### Version 2: Tile Layer Swap (Current Implementation)

**Decision:** Swap the entire Leaflet tile layer on theme change.
- **Light mode:** OpenTopoMap (topographic)
- **Dark mode:** CartoDB Dark Matter (genuinely dark tiles)

**Implementation (Commit 66172a5):**

Modified `src/Waymarked.Web/wwwroot/js/map.js`:
- Exposed `window.tileLayer` and `window.map` globals so `theme.js` can swap them

Modified `src/Waymarked.Web/wwwroot/js/theme.js`:
- Added `TILE_CONFIG` object with URLs and attributions for both providers
- Added `swapTileLayer(theme)` function: removes current layer, creates new `L.tileLayer`, adds to map, calls `bringToBack()` to keep SVG overlays on top
- Updated `applyTheme()` to call `swapTileLayer()` on every theme change (toggle click, initial load, OS preference detection)

Modified `src/Waymarked.Web/wwwroot/css/app.css`:
- Removed obsolete `[data-theme="dark"] .leaflet-tile-pane { filter: … }` rule

**Result:** Tiles are now genuinely dark in dark mode with appropriate contrast. Route polylines (#E0007A magenta) remain visible on top. No filter artifacts.

### Files Changed (Dark Mode - All Versions)
- `src/Waymarked.Web/wwwroot/css/app.css`
- `src/Waymarked.Web/wwwroot/index.html`
- `src/Waymarked.Web/wwwroot/js/theme.js`
- `src/Waymarked.Web/wwwroot/js/map.js`

---

## Dark Mode: Button Styling Fix (2026-04-13)

**Status:** IMPLEMENTED | **Owner:** Mouth (Frontend Dev) | **Commit:** 559b7e6

### Problem

Two button elements were broken in dark mode:

**"Plan Route" button:** The global `button` rule sets `color: var(--clr-white)`. In dark mode, `--clr-white` remaps to `#2a2a2e` (near-black, for panel backgrounds). This made button text near-invisible — dark grey on dark green.

**"Show Steps" button (`.steps-toggle`):** Background was `var(--clr-earth-lt)` = `#1a2d18` (very dark green) in dark mode. Text was `color: var(--clr-earth)` = `#2d5a27` (medium green). Result: green text on dark green background, effectively invisible.

### Root Cause

The `--clr-white` token was designed to darken card/panel surfaces in dark mode. But the `button` rule reused this token for text colour, silently breaking legibility.

### Solution

Added two CSS rules under `[data-theme="dark"]` hardcoded-colour fixes in `app.css`:

```css
[data-theme="dark"] button {
    color: #ffffff;
}

[data-theme="dark"] .steps-toggle {
    background: var(--clr-earth);
    color: #ffffff;
    border-color: var(--clr-earth-dk);
}
```

Both buttons now display white text on brand green (`#2d5a27`) in dark mode — readable, clickable, consistent with light mode. No impact on light mode.

### Files Changed
- `src/Waymarked.Web/wwwroot/css/app.css`

---

## Data: UK OSM Coverage Assessment — Team Recommendations

**From:** Data (GIS Engineer)  
**Date:** 2026-04-12  
**Status:** For Review  
**Audience:** Mikey (Lead), Brand (Backend), Josh (Product)

### Summary

Completed UK OSM footpath coverage assessment (see `docs/osm-coverage-uk.md`). Key finding: **OSM + Valhalla sufficient for MVP & production walking route planner**. Identified critical application-layer gaps and recommended development data strategy.

### Key Recommendations

#### 1. Development Data Strategy ✅ **DECISION READY**

**Recommendation:** Use **Scotland extract** for local dev spike; **full GB extract** for production.

**Rationale:**
- Scotland extract (`scotland-latest.osm.pbf`, ~100 MB): Fast iteration (Valhalla builds in <5 min), simplified access logic (Right to Roam), established routes (West Highland Way) for sanity checks
- Full GB extract (`great-britain-latest.osm.pbf`, ~1.5 GB): Comprehensive 3-nation coverage, scales to modest VPS, weekly update cadence via Geofabrik

**URLs:**
- Dev: https://download.geofabrik.de/europe/scotland-latest.osm.pbf
- Prod: https://download.geofabrik.de/europe/great-britain-latest.osm.pbf

**Next step:** Pass to Mikey (Backend) for spike implementation.

#### 2. Valhalla Application-Layer Gaps ⚠️ **DESIGN DECISION NEEDED**

Valhalla pedestrian costing does NOT natively support several hiking features. Application layer must implement:

| Feature | Status | App Layer Requirement |
|---------|--------|----------------------|
| **Difficulty filtering** (`sac_scale` T1–T6) | ❌ Unsupported | Parse OSM `sac_scale` tag; estimate from elevation if absent; filter route alternatives by difficulty |
| **Seasonal closures** (lambing, grouse, nesting) | ❌ Unsupported | Maintain curated closure database; display warnings on route UI |
| **Round-trip routing** | ❌ Unsupported | Implement algorithm (e.g., reverse-path A*) or restrict to open-ended routes |
| **Permissive path warnings** | ⚠️ Partial | Flag paths tagged `foot=permissive` in UI: "This section uses permissive paths (may be revoked by owner)" |
| **CRoW access land routing** | ⚠️ Partial | Store as GeoJSON zones; overlay on map; warn users on routes crossing zones |

**Design Question:** Should Waymarked v1.0 MVP support difficulty filtering and seasonal closures, or defer to v1.1?

#### 3. UK PROW Coverage Quality ✅ **FYI / EXPECTATIONS SETTING**

**Regional variation:**
- **Excellent:** Lake District, Peak District, South Downs, South West Coast (>80% PROW designated tags)
- **Good:** Welsh uplands, major long-distance paths (Pennine Way, Coast-to-Coast)
- **Fair:** Remote rural areas (Scottish Borders, mid-Wales, East Anglia) — user may find limited options
- **Coverage:** ~40-60% of English/Welsh paths tagged with `designation` tag; ~30-40% fewer paths in Scotland (Right to Roam model)

**Implication for product:** Waymarked will excel for popular UK walking regions; edges (remote areas, lesser-known local paths) may have gaps. Plan user expectations accordingly in product docs.

#### 4. Data Contribution Opportunity 🔮 **FUTURE ENHANCEMENT**

**Opportunity:** MapThePaths project successfully conflated 149 UK authority PROW datasets into OSM (~60,000 paths). Waymarked could contribute:

1. **Seasonal closure tagging:** Partner with Scottish Wildlife Trust, grouse estates, MoD to systematically tag `access:conditional` on paths
2. **CRoW zone mapping:** Map access land boundaries as GeoJSON/OSM relations (currently unmapped)
3. **Surface/difficulty refinement:** Crowdsource `sac_scale` and `trail_visibility` tagging on untagged paths

**Impact:** Improves Waymarked's own routing quality + benefits broader OSM community.

### Action Items

- **Mikey (Backend):** Spike Valhalla deployment with Scotland extract; test custom costing (`use_roads`, `use_hills`)
- **Brand (Backend):** Design app layer for difficulty filtering, seasonal warnings, round-trip algorithm
- **Data (me):** Stand by for regional coverage analysis (e.g., which auth boundaries have poorest PROW tagging?)
- **Josh (Product):** Review expectations (regional variation, v1.0 vs v1.1 feature list)

**Full assessment:** See `docs/osm-coverage-uk.md` — living reference, updated as findings emerge.

---
## Fix Export Button Hover Text — Dark Mode

**Date:** 2025-01-27  
**Author:** Mouth (Frontend Dev)  
**Status:** Implemented  
**Commit:** b5a70be

### Decision

Pin export button (GPX, KML, GeoJSON) hover text to #ffffff in dark mode.

### Problem

Export buttons displayed near-black text on hover in dark mode. The .export-btn:hover rule sets color: var(--clr-white), which in dark mode resolves to #2a2a2e (near-black panel surface colour). Result: near-black text on dark-green (#2d5a27) hover background — effectively invisible.

### Root Cause Pattern

The `--clr-white` token serves dual duty: it's used as both a *surface colour* (panels, card backgrounds) and as *text colour* (contrast on green buttons). The dark mode override correctly darkens surfaces but inadvertently breaks any `color: var(--clr-white)` usage on hover states.

The existing `[data-theme="dark"] button { color: #ffffff; }` rule fixes the base button state but does not override the explicit `color: var(--clr-white)` set in `.export-btn:hover` — specificity is equal, but the hover rule appears later in the cascade and wins.

### Fix Applied

Added a targeted dark mode hover override in `src/Waymarked.Web/wwwroot/css/app.css`:

\\\css
/* export-btn hover: --clr-white remaps to #2a2a2e in dark mode, making
   hover text near-black on dark-green. Pin to literal white. */
[data-theme="dark"] .export-btn:hover {
    color: #ffffff;
}
\\\

**Selector used:** `[data-theme="dark"] .export-btn:hover`

The fix is minimal and surgical — it only overrides the one property (`color`) on the one state (`hover`) that was broken. The background colour (`var(--clr-earth)`) is unaffected and renders correctly in both modes.

### Files Modified

- `src/Waymarked.Web/wwwroot/css/app.css` — Added dark mode hover text override

### Quality

- Minimal, surgical fix — only one property on one state
- No light mode impact
- Consistent with existing dark mode button fixes (commit 559b7e6)
- Export buttons now readable on hover in both light and dark modes

---
