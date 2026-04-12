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
