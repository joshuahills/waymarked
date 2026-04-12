# Orchestration Log: brand-graphhopper-spike

**Agent:** Brand (Backend Development)  
**Timestamp:** 2026-04-12T17:00:00Z → 2026-04-12T17:30:00Z  
**Status:** COMPLETED

---

## Task

Spike GraphHopper routing engine integration with UK OSM data. Validate routing profiles, elevation handling, and provide Docker Compose reference implementation for Aspire migration.

---

## What Was Delivered

### Artifacts

Created `infra/graphhopper/` with:
- `docker-compose.yml` — Single GraphHopper service, ports 8989, volume mounts for OSM data and graph cache
- `config.yml` — Routing profiles (`foot`, `hike`), SRTM elevation, UK bounding box
- `README.md` — Setup, profile explanations, memory requirements, gotchas
- `test-route.sh` & `test-route.ps1` — Edinburgh Waverley → Arthur's Seat test cases
- `.gitignore` — Standard Docker/Java exclusions

### Technical Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| **Dev OSM Extract** | Scotland (`scotland-latest.osm.pbf`, ~200MB) | Fast builds (5-10 min), lower RAM (2-3GB), representative terrain, faster iteration |
| **Production Extract** | Full GB (`great-britain-latest.osm.pbf`, ~1.5GB) | Comprehensive 3-nation coverage, scales to modest VPS |
| **Elevation Provider** | SRTM (auto-download) | Zero setup, sufficient for spike; defer OS Terrain 50 to production |
| **Routing Profiles** | `foot` + `hike` (both enabled) | Urban routes (foot), countryside routes (hike); UI will need mode selector |
| **Memory Config** | `-Xmx4g -Xms1g` (default) | Middle ground for Scotland + regional extracts; documentation provided for scaling |

### Key Findings

1. **Profile Behavior:** `foot` uses roads if faster; `hike` avoids roads. Need UI mode differentiation.
2. **Round-Trip Support:** GraphHopper natively supports (unlike Valhalla) — validates team routing engine choice.
3. **Elevation Trade-off:** SRTM (90m) acceptable for spike; OS Terrain 50 (50m) prioritized for production.

### Open Questions

- OSM footpath coverage quality in remote areas (Scottish Borders, access land)
- Graph update frequency (weekly/monthly)
- Custom weighting for scenic routes (forest, parks, water)
- Production deployment: single instance vs horizontal scaling

---

## Next Steps

1. Aspire migration: Use Docker Compose as reference for GraphHopper container resource
2. Route quality validation: Test 10-20 UK routes against Google Maps / OS Maps
3. OS Terrain 50: Document setup for production
4. Load testing: Benchmark concurrent request capacity

---

## Handoff

**To:** Brand (Aspire spike), Mikey (Backend integration), Data (coverage analysis)

**Usefulness for Aspire:** High — Docker setup translates directly to Aspire container resource configuration.

---
