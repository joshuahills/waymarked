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
