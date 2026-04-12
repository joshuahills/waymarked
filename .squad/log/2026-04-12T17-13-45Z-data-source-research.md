# Session Log: Data Source Research — 2026-04-12T17:13:45Z

## Research Objective
Comprehensive evaluation of UK geospatial data sources suitable for a walking/hiking/running route planner application, spanning routing data, elevation models, basemap tiles, and geocoding services.

## Research Scope
- **Routing data sources** (OpenStreetMap, Ordnance Survey, PROW authorities)
- **Elevation data** (OS Terrain 50, SRTM, Copernicus DEM, OS Terrain 5)
- **Basemap tile providers** (OpenTopoMap, Thunderforest, OS Maps, Mapbox, Stadia)
- **Geocoding services** (Nominatim, Photon, OS Names, commercial providers)
- **Routing engines** (OSRM, Valhalla, GraphHopper)
- **UK-specific access rights** (PROW, Right to Roam, seasonal restrictions)

## Key Findings

### Routing Data
**OpenStreetMap** is the clear choice for UK routing backbone:
- Excellent UK footpath/PROW coverage via OSM community and MapThePaths project
- Critical tags: `highway=footway|path|bridleway`, `designation=public_footpath`, `foot=*`, `surface=*`, `sac_scale=*`
- ODbL licence: free for commercial use with attribution and share-alike for derived databases
- Real-time updates via changesets; weekly planet dumps
- Quality varies by region but adequate for walking routes

OS OpenData provides elevation and basemap but **no routing-grade footpath data**. OS premium (MasterMap, NGD APIs) is authoritative but cost-prohibitive (~£1000s/year).

### Elevation Data
**OS Terrain 50** is the recommended primary source:
- 50m resolution, free (OGL licence), authoritative for UK
- Superior to SRTM (90m) for UK terrain detail
- Vertical accuracy ±2.5m RMSE
- Easy download from OS Data Hub

SRTM (90m) and Copernicus DEM (30m) available as fallbacks; Copernicus DEM better for international expansion.

### Basemap Tiles
- **MVP:** OpenTopoMap (free, topographic, no SLA)
- **Production:** Thunderforest Outdoors ($29-500+/month, purpose-built for hiking, commercial SLA)
- **Premium option:** OS Maps API tiles for users wanting official OS Explorer style
- Self-hosted tiles viable at scale (~$50-150/month VPS)

### Geocoding
- **MVP:** Photon or Nominatim public instances (free, rate-limited)
- **Production:** Self-hosted Nominatim (~$50-100/month) or commercial provider (LocationIQ, OpenCage, Geoapify)
- OS Names API available as UK premium option (~£100s/month)

### Routing Engine (per this research)
**GraphHopper recommended:**
- Java-based, easier to customize than C++ (OSRM/Valhalla)
- Excellent hiking profiles with native elevation integration
- Route diversity support (alternative routes)
- Active community; well-documented

OSRM fast but less flexible for hiking. Valhalla good but more complex setup.
*Note: Mikey's research recommends Valhalla — team needs to reconcile.*

### UK-Specific Access Rights
**England & Wales:** PROW system with clear designations; permissive paths; Access Land (CRoW Act)
**Scotland:** Right to Roam model (broader access rights); PROW concept less relevant
**Seasonal restrictions:** Nesting (Mar-Aug), lambing (Mar-May), grouse shooting (Aug-Dec), military ranges, tidal access — NOT in static datasets

### Cost Estimates
- **MVP stack:** ~$20-50/month (routing engine VPS; all data sources free)
- **Production stack:** ~$150-400/month (routing, tiles, geocoding scaled)

## Recommendations
1. Use OpenStreetMap + GraphHopper for routing MVP
2. Integrate OS Terrain 50 for elevation profiles
3. Start with OpenTopoMap tiles; plan migration to Thunderforest
4. Use Photon/Nominatim for geocoding; scale to self-hosted or commercial
5. Leverage open PROW data (rowmaps.com) for PROW verification/supplementation
6. Plan for seasonal closure data partnerships (future enhancement)

## Open Questions / Risks
- OSM PROW coverage completeness varies by region
- Free tile/geocoding services have no SLA; plan for migration
- Seasonal access data unavailable; requires partnerships/UGC
- Team decision needed: Valhalla vs GraphHopper for routing engine

## Session Duration
Research completed 2026-04-12.

## Data Artifacts
- `.squad/decisions/inbox/data-uk-data-sources.md` — Full 468-line detailed analysis
