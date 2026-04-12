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
