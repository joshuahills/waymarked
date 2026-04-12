# Mikey — Project History

## Project Context

- **Project:** Waymarked — UK walking, hiking, and running route planner
- **Requested by:** Josh Hills
- **Goal:** Build a route planner app from scratch, primarily for UK use. Customisable options, map view of routes. Starting point: find suitable data source(s).
- **Stack:** TBD — no tech decisions made yet. Data source research is the first priority.
- **Status:** Team hired, work beginning.

## Key Files

_(none yet — project just started)_

## Learnings

### 2026-04-12: Routing Engine Research

**Evaluated routing engines for UK walking/hiking/running:**

**Valhalla (RECOMMENDED):**
- MIT license (permissive, commercial-friendly)
- Excellent pedestrian/hiking profiles with elevation support built-in (Skadi module)
- Uses OSM data — UK has exceptional OSM footpath coverage
- Self-hostable via Docker, low resource requirements for UK data (~1.5GB OSM extract, 4-8GB RAM)
- Active development, production-ready (used by Mapbox, Stadia Maps)
- Node.js and Python bindings available
- Built-in: isochrones, map-matching, elevation sampling, turn-by-turn
- UK + Ireland graph builds in minutes, millisecond route responses

**GraphHopper (FALLBACK):**
- Apache 2.0 license (permissive)
- Excellent custom model support, has built-in `hike.json` profile
- Strong round-trip route generation (Valhalla lacks this natively)
- Java-based (heavier stack than Node.js)
- API pricing escalates quickly for commercial use
- Good self-hosting option but less documented than Valhalla

**OpenRouteService (REJECTED):**
- GPL-3.0 license (copyleft — forces open-sourcing modifications, dealbreaker for commercial)
- Fork of GraphHopper, Java/Spring Boot stack
- Excellent API for free public use, but licensing blocks commercial self-hosting
- Built for humanitarian/academic use

**OSRM (NOT SUITABLE):**
- BSD-like license (permissive)
- Car-focused routing, basic pedestrian profiles
- No built-in elevation support
- Contraction Hierarchies make runtime customization difficult
- Wrong tool for walking/hiking use case

**Key Technical Decisions:**
- Backend: Node.js + Express (aligns with Valhalla Node.js bindings, faster iteration)
- Frontend: React + Mapbox GL JS (best performance for vector tiles, free tier sufficient initially)
- Mapping data: OSM via Mapbox vector tiles or self-hosted OpenMapTiles
- Deployment: Docker-based Valhalla on single VPS initially, scale horizontally as needed

**Critical Path Dependencies:**
- Data (GIS Engineer) must validate UK OSM footpath coverage, surface tag prevalence, elevation data accuracy
- Need to confirm Public Rights of Way, coastal paths, and National Trails are properly tagged
- Elevation: SRTM 90m may need upgrade to OS Terrain 50 for accuracy (licensing/cost TBD)
- Round-trip routing: Valhalla lacks native support — may need custom algorithm or GraphHopper integration

**Risks Identified:**
- OSM data quality for UK footpaths — requires spot-checking (Data team)
- Round-trip route generation — Valhalla doesn't support, GraphHopper does (potential hybrid approach)
- Multi-day route planning with accommodation — needs custom logic
- Weekly OSM updates require blue/green deployment to avoid downtime

**Next Actions:**
- Data validates UK OSM coverage within 1 week
- Spike: Deploy Valhalla with UK extract, test pedestrian routing + custom costing
- Spike: Test "avoid roads", "prefer scenic", elevation-based routing
- Decision: If OSM insufficient, evaluate contributing to OSM vs. hybrid OS MasterMap approach
