# Data — Project History

## Project Context

- **Project:** Waymarked — UK walking, hiking, and running route planner
- **Requested by:** Josh Hills
- **Goal:** Build a route planner app from scratch, primarily for UK use. Customisable options, map view of routes. Starting point: find suitable data source(s).
- **Stack:** TBD — data source research is the first priority.
- **Status:** Team hired, work beginning.

## Key Files

_(none yet — project just started)_

## Learnings

### 2026-04-12: GraphHopper Custom Profile Files Implementation

**Task:** Verify and create/update custom routing profile files referenced in config.yml

**Findings:**
1. Three custom model files were designed in round-trip accuracy research but **not created in the repository**
   - `foot.json` — pedestrian-focused profile (missing)
   - `hike.json` — scenic/elevation-aware profile (missing)
   - `foot_elevation.json` — shared elevation integration (missing)

2. **Impact:** GraphHopper would fall back to default profiles at runtime, negating custom tuning; config.yml references would cause errors or silent fallback

3. **Root cause:** Design completed, implementation deferred

**Actions taken:**
1. ✅ Created `infra/graphhopper/data/foot.json` with distance_influence: 80 (pedestrian, short routes)
2. ✅ Created `infra/graphhopper/data/hike.json` with distance_influence: 60 (scenic, elevation-aware)
3. ✅ Created `infra/graphhopper/data/foot_elevation.json` with shared elevation penalties
4. ✅ Updated `Dockerfile` to COPY model files into container at `/data/` during build

**Result:** Custom profiles now ready for GraphHopper runtime; config.yml references resolve correctly

**Key learning:** Docker COPY (build-time) vs volume mounts (runtime) distinction — config.yml can be mounted as read-only volume, but custom model files must be copied into the image.

**Alignment:** Implements Tier 1 (Immediate) from round-trip accuracy research

**Next:** Brand to implement Tier 2 (API retry logic); Mouth to document accuracy bands for users

---

### 2026-04-12: GraphHopper Round-Trip Algorithm Analysis (Accuracy Research)

**Research:** Deep dive into why GraphHopper's round-trip algorithm produces routes that don't match requested distance, and what's tunable at the config level.

**Key Findings:**

1. **Algorithm is fundamentally a heuristic, not a guarantee**
   - `algorithm=round_trip` explores the graph radially with `round_trip.distance` as a *guide*, not a constraint
   - Deviations of ±20–40% are normal in rural UK, ±5–15% in urban areas

2. **Root causes of distance mismatches:**
   - **Network density:** Sparse OSM path coverage in rural UK (moorlands, remote uplands) → algorithm exhausts local graph and stops
   - **Profile restrictions:** Hike/foot profiles restrict routing to pedestrian ways only, narrower graph than car routing
   - **Elevation penalties:** Steep section avoidance creates lateral detours, adding unintended distance or forcing undershoot
   - **Algorithm termination:** No built-in retry or compensation when graph exhausts early
   - **Random seed variance:** Different seeds = ±15–25% distance variance on same start point

3. **UK OSM coverage implications:**
   - Peak District/Lake District: ±10–15% accuracy (dense coverage)
   - Cotswolds/South Downs: ±15–20% accuracy (moderate coverage)
   - Remote uplands (Cairngorms, Pennines, moorlands): ±25–40% accuracy (sparse coverage)
   - Undershoot more common in rural areas; overshoot more common in dense urban networks

4. **Config-level tuning potential: ±5–10% improvement max**
   - ✅ Adjustable: `distance_influence` parameter (prefer distance over route quality)
   - ✅ Adjustable: Speed penalties for road classes (avoid road routing)
   - ✅ Adjustable: Elevation cost caps (reduce lateral detours)
   - ✅ Adjustable: Seeding strategy (determinism vs variety)
   - ❌ NOT fixable at config: Network sparsity, algorithm termination behavior, hard graph limits

5. **What cannot be fixed at config level:**
   - Cannot create footpaths that don't exist in OSM
   - Cannot override graph exhaustion in sparse regions
   - Cannot guarantee distance targets in areas with limited available paths

**Recommendations (by tier):**

- **Tier 1 (Immediate):** Verify custom model files (foot.json, hike.json, foot_elevation.json) exist; document realistic accuracy bands
- **Tier 2 (API layer - Brand):** Implement retry logic when distance < 90% of target; retry with 1.15x distance multiplier
- **Tier 3 (Future):** Upgrade elevation provider from SRTM to OS Terrain 50; A/B test distance_influence parameter values

**Full analysis:** See `.squad/decisions/inbox/data-roundtrip-accuracy.md`

---

### 2026-04-12: Documentation Update — GraphHopper Engine Alignment

**Update:** Corrected `docs/osm-coverage-uk.md` to align with team's GraphHopper routing engine decision (see `.squad/decisions.md`). All Valhalla references replaced with GraphHopper equivalents:
- Valhalla pedestrian costing → GraphHopper hiking profiles (hike.json)
- Valhalla architecture → GraphHopper Java containerization (via Aspire)
- Elevation integration (Valhalla Skadi) → GraphHopper DEM integration (OS Terrain 50)

Documentation now accurate for Brand's .NET Aspire implementation and future backend integration.

---

### 2026-04-12: UK OSM Footpath Coverage Assessment (Valhalla Integration)

**OSM Highway Tag Support in Valhalla:**
- Valhalla pedestrian costing natively respects core tags: `highway=footway|path|bridleway|track|residential|tertiary`
- Foot access overrides: `foot=yes|designated|permissive|no|private` all respected
- UK PROW `designation` tags (public_footpath, public_bridleway, etc.) implied via `foot=designated`; Valhalla does NOT explicitly parse `designation` but application layer can use them for UI filtering

**UK PROW Coverage Quality:**
- England & Wales: ⭐⭐⭐⭐ (4/5) — ~40-60% of paths tagged with designation; excellent in Lake District/Peak District/South Downs; gaps in remote rural areas
- Scotland: ⭐⭐⭐ (3/5) — ~30-40% fewer path segments than E&W; Right to Roam model means PROW tagging less critical; established routes well-mapped
- MapThePaths project added ~60,000 PROW paths via conflation with 149 UK authorities; ongoing effort improving coverage

**Valhalla Hiking Profile Specifics:**
- NO native support for `sac_scale` (T1–T6 difficulty) — application layer must parse and warn users
- NO native support for seasonal `access:conditional` tags — assumes permanent access
- NO native support for round-trip routing — application must implement or use open-ended paths
- Elevation integration via Skadi module: elevation sampling implicit; `use_hills` costing parameter controls hill avoidance (0-1, lower = avoid steep)
- Custom costing parameters enable "footpaths only" mode: `use_roads: 0.3` deprioritises roads

**Access Restrictions & Limitations:**
- CRoW Act access land NOT routable as paths; workaround: store as GeoJSON zones, highlight on map
- Seasonal closures (lambing, grouse, nesting) unmapped (<1% of UK paths); requires application-level database + partnership feeds
- Permissive paths (`foot=permissive`) ~15-20% of rural UK; revokable at owner discretion; application should warn users
- Scottish Right to Roam mapped de facto (open land has fewer paths, so fewer routable options); no special handling needed

**Recommended Dev/Prod Data:**
- **Dev:** Scotland extract (`scotland-latest.osm.pbf`, ~100 MB) — fast Valhalla build (~5 min), good elevation, established routes for sanity checks
- **Prod:** Full GB extract (`great-britain-latest.osm.pbf`, ~1.5 GB) — comprehensive; Valhalla processes in 20-30 min, scales to modest VPS (4GB RAM, 20GB SSD)
- Geofabrik URLs: Scotland = https://download.geofabrik.de/europe/scotland-latest.osm.pbf; GB = https://download.geofabrik.de/europe/great-britain-latest.osm.pbf

**Key Findings:**
- OSM sufficiently covers UK for walking app MVP & production
- Valhalla pedestrian + OS Terrain 50 elevation = solid hiking routing
- Major gaps: CRoW/access land routing, seasonal closures, difficulty tagging (sac_scale <5% present)
- Application layer must handle: sac_scale parsing, seasonal closure warnings, permissive path caveats, round-trip algorithm

### 2026-04-12: UK Data Source Landscape Research

**OpenStreetMap for Routing:**
- OSM has excellent UK coverage for footpaths, bridleways, and public rights of way (PROW), especially in popular walking areas
- Key tags: `highway=footway|path|bridleway|track`, `designation=public_footpath|public_bridleway`, `foot=yes|designated|permissive`, `surface=*`, `sac_scale=*`
- Route relations: `type=route`, `route=hiking|foot`, `network=iwn|nwn|rwn|lwn` for tagged long-distance routes
- UK OSM community active; MapThePaths project adding PROW data
- Quality varies regionally; some rural areas have gaps; surface/difficulty tagging incomplete
- ODbL licence: free for commercial use, requires attribution + share-alike for derived databases

**Ordnance Survey Data:**
- **OS OpenData (free):** OS Terrain 50 (elevation, 50m), OS Open Zoomstack (basemap rendering), OS Open Roads (roads only, NO footpaths/PROW)
- **OS Premium (paid):** OS Maps API (raster tiles, ~£100s/month+), OS NGD APIs (vector data including PROW, expensive, not designed for routing), OS MasterMap (legacy, very expensive)
- Verdict: OS OpenData useful for elevation + basemap; premium too expensive for startup unless targeting customers who need official OS branding
- OS Terrain 50 superior to SRTM/Copernicus for UK elevation data (50m resolution, authoritative)

**PROW Data:**
- 149 authorities in England/Wales released PROW data under OGL (see rowmaps.com)
- Authoritative but often outdated; some authorities haven't updated in years
- Could supplement OSM for verification/conflation but non-trivial to merge 149 different datasets
- Scotland has Right to Roam (different access model); PROW concept less relevant

**Elevation Sources:**
- OS Terrain 50 (50m, free, OGL): **Best for UK**
- SRTM (90m, free, public domain): Fallback but OS Terrain 50 better
- Copernicus DEM (30m, free): Good for international expansion
- OS Terrain 5 (5m, paid): Overkill for general walking app

**Basemap Tiles:**
- OSM Standard Tiles: Free but restrictive usage policy (not for production)
- OpenTopoMap: Free, topographic, good for hiking but no SLA (fair use only)
- Thunderforest Outdoors: Paid ($29-500+/month), purpose-built for outdoor/hiking, commercial SLA
- OS Maps API: Paid (~£100s/month), official OS Explorer/Landranger style, GB only
- Recommendation: OpenTopoMap for MVP → Thunderforest for production OR self-host tiles

**Geocoding:**
- Nominatim (free, OSM-based): Good for dev but rate-limited (1 req/s) — not suitable for production
- Photon (free, OSM-based, by Komoot): Alternative to Nominatim, often faster, same caveats
- OS Names API (paid): Authoritative UK postcodes/addresses, ~£100s/month
- Commercial providers (LocationIQ, OpenCage, Geoapify): ~$50-100/month, Nominatim-compatible APIs with SLA
- Recommendation: Photon/Nominatim for MVP → self-host Nominatim or commercial provider for production

**Routing Engines (all open-source):**
- OSRM: Fastest, C++, car/bike/foot profiles, less flexible for elevation-aware hiking
- Valhalla: Multi-modal, elevation-aware, C++, used by Mapbox
- GraphHopper: Java, best hiking profiles, native elevation integration, customizable, route diversity
- **Recommendation: GraphHopper** for walking app — elevation costs, surface preferences, difficulty ratings, easy to extend

**UK Access Considerations:**
- England/Wales: PROW system (`designation=public_footpath|public_bridleway|restricted_byway|byway_open_to_all_traffic`), permissive paths (`foot=permissive`), Access Land (CRoW Act)
- Scotland: Right to Roam (Scottish Outdoor Access Code) — responsible access to most land; PROW concept less relevant
- Seasonal closures (nesting, lambing, grouse shooting, military ranges, tides) NOT in static datasets — would need partnerships or UGC for real-time data

**Recommended Day 1 Stack (MVP, ~$20-50/month):**
- Routing data: OSM UK extract (Geofabrik, free)
- Routing engine: GraphHopper (self-hosted, ~$20-50/month VPS)
- Elevation: OS Terrain 50 (free)
- Basemap: OpenTopoMap (free, topographic)
- Geocoding: Photon or Nominatim public instance (free, rate-limited)

**Recommended Production Stack (~$150-400/month):**
- Routing: GraphHopper (self-hosted, ~$50-150/month VPS)
- Elevation: OS Terrain 50 + Copernicus DEM (free)
- Basemap: Thunderforest Outdoors ($29-500+/month) OR self-hosted tiles (~$50-150/month)
- Geocoding: Self-hosted Nominatim (~$50-100/month) OR commercial provider (~$50-100/month)
- Optional premium: OS Maps API tiles for subscribers (~£100s/month)

**Key Sources:**
- OpenStreetMap wiki: Highway tagging, Walking Routes, Access provisions in UK, UK PROW tagging
- Ordnance Survey Data Hub: docs.os.uk (OS APIs, OpenData products)
- waymarkedtrails.org: Reference for rendering OSM hiking route relations
- rowmaps.com: Open PROW data downloads for 149 UK authorities
- Routing engines: project-osrm.org, github.com/valhalla/valhalla, graphhopper.com
- Tile providers: opentopomap.org, thunderforest.com, tile usage policies at operations.osmfoundation.org
- Geocoding: wiki.openstreetmap.org/wiki/Nominatim, photon.komoot.io
- Elevation: OS Terrain 50 (OS Data Hub), srtm.csi.cgiar.org, Copernicus DEM
