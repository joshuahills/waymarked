# Squad Decisions

## Active Decisions

### 2026-04-12: Routing Engine and Architecture Selection

**Status:** Ready for Review | **Owner:** Mikey (Lead) | **Input:** Data (GIS Engineer)

#### Decision: Use Valhalla as primary routing engine with Node.js/Express backend and React/Mapbox GL frontend

**Reasoning:**
Valhalla is the optimal choice for UK walking/hiking/running route planning:
- **MIT license** (permissive, no commercial restrictions)
- **Pedestrian/hiking profiles** with built-in elevation sampling (Skadi module)
- **Self-hostable** — UK OSM extract (~1.5GB) runs on modest hardware (4-8GB RAM)
- **Production-proven** — used by Mapbox, Stadia Maps
- **Elevation-aware** — critical for hill/peak avoidance and scenic preferences
- **Isochrones & map-matching** — valuable for "how far in X time" and GPS trace cleanup

**Architecture:**
- **Backend:** Node.js + Express (Valhalla has excellent Node.js bindings; Python alternative via FastAPI)
- **Frontend:** React + Mapbox GL JS (best vector tile performance, free tier 50k loads/month)
- **Deployment:** Docker-based Valhalla on single VPS initially; scale horizontally as needed
- **Mapping data:** OSM via Mapbox vector tiles or self-hosted OpenMapTiles

**Route Customization Mapping:**
- **Surface type:** `use_roads`, `use_tracks` costing weights
- **Avoid roads:** `use_roads: 0`
- **Avoid hills:** `max_grade` + elevation penalties
- **Prefer scenic:** Weighted by `surface`, `park`, `forest` tags
- **Waypoints:** Standard Valhalla location array support
- **Elevation profiles:** Via Skadi elevation sampling

**Alternatives Evaluated:**
- **GraphHopper:** Apache 2.0 (good), excellent hiking profiles, Java-based (heavier), API pricing escalates, excellent round-trip support
- **OSRM:** BSD license (good), car-focused, limited pedestrian profiles, no elevation support
- **OpenRouteService:** GPL-3.0 (hard no for commercial), forked from GraphHopper

**Dependencies:**
- Data validation of UK OSM footpath coverage (footpaths, surface tags, elevation accuracy) — critical path
- Spike: Deploy Valhalla with UK extract, test pedestrian routing + custom costing
- **Risk:** Round-trip routing — Valhalla lacks native support; may need custom algorithm or GraphHopper fallback

**Next Steps:**
1. Data validates UK OSM coverage within 1 week
2. Backend team spikes Valhalla deployment with UK extract
3. Test custom costing: "avoid roads", "prefer scenic", elevation-based routing
4. Decide: If OSM insufficient, evaluate contribution to OSM vs hybrid OS MasterMap approach

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

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
