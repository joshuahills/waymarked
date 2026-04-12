# Session Log: GraphHopper Routing Engine Decision

**Date:** 2026-04-12T18:34:33Z

## Summary

Josh Hills confirmed GraphHopper as the primary routing engine for Waymarked, superseding the earlier Valhalla vs GraphHopper decision fork.

## Key Points

- **Decision Maker:** Josh Hills
- **Decision:** GraphHopper (Apache 2.0 license)
- **Rationale:** 
  - Superior hiking profiles (hike.json)
  - Native round-trip/circular route generation (advantage over Valhalla)
  - Reasonable infrastructure costs (~€19/month on Hetzner CX42)
  - Proven production implementation
- **Licensing:** Apache 2.0 (commercial-friendly)

## Decisions Merged

- Merged `coordinator-graphhopper-decision.md` inbox file into main decisions.md
- Updated routing engine section to reflect LOCKED status with GraphHopper as final selection
- Superseded previous Valhalla decision from 2026-04-12 Mikey/Data research sprint

## Parallel Work Spawned

- **brand-graphhopper-spike** (background): Create Docker spike for GraphHopper with UK OSM extract
- **data-osm-coverage** (background): Validate UK OSM footpath coverage for routing accuracy

## Architecture Implications

- Round-trip/circular routing now natively supported (key for hiking routes)
- Custom hiking profiles available via hike.json
- Integration point: Node.js GraphHopper client bindings
- Elevation integration: OS Terrain 50 for UK
