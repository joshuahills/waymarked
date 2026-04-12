# GraphHopper Local Development Spike

This directory contains a Docker Compose setup for running GraphHopper with UK OSM data, configured for hiking and walking route planning.

## Overview

- **Routing Engine:** GraphHopper (Apache 2.0)
- **Profiles:** `foot` (general walking + footpaths) and `hike` (prefers hiking trails, avoids roads)
- **Elevation:** SRTM (auto-downloaded, ~90m resolution)
- **API Port:** 8989

## Quick Start

### 1. Download OSM Extract

**For development/testing (recommended):**
```bash
# Scotland only (~200MB) - faster graph build, lower memory usage
mkdir -p data
curl -L -o data/map.osm.pbf https://download.geofabrik.de/europe/great-britain/scotland-latest.osm.pbf
```

**For full UK coverage:**
```bash
# Great Britain full (~1.5GB) - slower graph build, higher memory requirements
mkdir -p data
curl -L -o data/map.osm.pbf https://download.geofabrik.de/europe/great-britain-latest.osm.pbf
```

### 2. Start GraphHopper

```bash
docker-compose up -d
```

**Initial startup:** The first run will:
1. Parse the OSM extract
2. Build the routing graph (this takes time - see below)
3. Download elevation data (SRTM tiles for UK)
4. Start the API server

**Monitor progress:**
```bash
docker-compose logs -f graphhopper
```

### 3. Wait for Graph Build

**Build times (approximate):**
- **Scotland extract:** 5-10 minutes (2-3GB RAM required)
- **Full GB extract:** 20-40 minutes (8-12GB RAM required)

You'll see log messages like:
- `finished way processing` 
- `creating CH graphs`
- `GraphHopper loaded in XXs`

Once you see `Started server at HTTP 8989`, the API is ready.

### 4. Test Routing

**Bash/Linux/macOS:**
```bash
./test-route.sh
```

**PowerShell/Windows:**
```powershell
.\test-route.ps1
```

Or manually:
```bash
curl "http://localhost:8989/route?point=55.9533,-3.1883&point=55.9444,-3.1618&profile=hike&points_encoded=false&elevation=true" | jq
```

### 5. Stop GraphHopper

```bash
docker-compose down
```

The graph cache in `./data/graph-cache/` persists, so subsequent starts are much faster.

## Configuration

### Profiles

- **`foot`:** General walking profile
  - Uses road network + footpaths
  - Suitable for urban walking, general hiking
  - Balances speed and accessibility
  
- **`hike`:** Hiking-focused profile
  - Prefers dedicated hiking paths and trails
  - Avoids roads where possible
  - Better for countryside/mountain routes
  - Uses SAC scale (`sac_scale` OSM tag) for difficulty

### Elevation

Currently using **SRTM** (90m resolution, auto-downloaded):
- Pros: Zero setup, works globally
- Cons: Lower resolution than OS Terrain 50 (~50m)

**To use OS Terrain 50 (better UK accuracy):**
1. Download OS Terrain 50 from Ordnance Survey
2. Convert to GraphHopper-compatible format (GeoTIFF)
3. Update `config.yml`: change `graph.elevation.provider` to `cgiar` or `gmted`
4. Mount elevation files into container

### Memory Requirements

Adjust `JAVA_OPTS` in `docker-compose.yml` based on your extract:

| Extract | Graph Size | Recommended Xmx | Minimum RAM |
|---------|------------|-----------------|-------------|
| Scotland | ~1-2GB | 2-3GB | 4GB |
| Great Britain | ~4-6GB | 8-12GB | 16GB |
| England | ~3-4GB | 6-8GB | 12GB |

**Symptoms of insufficient memory:**
- `OutOfMemoryError` in logs
- Container crashes during graph build
- Very slow graph creation

**Fix:** Increase `-Xmx4g` value in `docker-compose.yml` (e.g., `-Xmx8g` for 8GB heap).

## API Examples

### Basic Route (Edinburgh to Arthur's Seat)
```bash
curl "http://localhost:8989/route?point=55.9533,-3.1883&point=55.9444,-3.1618&profile=hike&points_encoded=false&elevation=true"
```

### Multi-waypoint Route
```bash
curl "http://localhost:8989/route?point=55.9533,-3.1883&point=55.9500,-3.1700&point=55.9444,-3.1618&profile=foot"
```

### Route with Instructions
```bash
curl "http://localhost:8989/route?point=55.9533,-3.1883&point=55.9444,-3.1618&profile=hike&instructions=true&locale=en-GB"
```

### Alternative Routes
```bash
curl "http://localhost:8989/route?point=55.9533,-3.1883&point=55.9444,-3.1618&profile=hike&algorithm=alternative_route&alternative_route.max_paths=3"
```

## Gotchas & Known Issues

### 1. Graph Build Disk I/O
The initial graph build is **very disk-intensive**. On slower disks (spinning HDDs), build times can be 2-3x longer. Use SSD if possible.

### 2. Graph Cache Invalidation
If you change `config.yml` profiles or elevation settings, delete the graph cache to rebuild:
```bash
docker-compose down
rm -rf data/graph-cache
docker-compose up -d
```

### 3. Elevation Download Failures
SRTM downloads from NASA can be slow or fail. If stuck:
- Check logs for `elevation` messages
- Retry: SRTM tiles are cached in `data/elevation-cache/` after first download
- Alternative: Pre-download SRTM tiles manually

### 4. Port Already in Use
If port 8989 is taken, edit `docker-compose.yml`:
```yaml
ports:
  - "9999:8989"  # Use port 9999 externally
```

### 5. Profile Differences: `foot` vs `hike`

Key behavioral differences:
- **`foot`** will route along roads if faster (e.g., A-roads, B-roads with footpaths)
- **`hike`** strongly prefers trails, even if slower
- For urban routes or mixed terrain → use `foot`
- For countryside/mountain routes → use `hike`
- Test both profiles for your use case

### 6. Round-Trip Routing
GraphHopper supports round-trip routing natively (unlike Valhalla):
```bash
curl "http://localhost:8989/route?point=55.9533,-3.1883&round_trip.distance=10000&round_trip.seed=0&profile=hike"
```

## API Documentation

Full GraphHopper API docs: https://docs.graphhopper.com/#tag/Routing-API

Interactive API explorer: http://localhost:8989/ (web UI)

## Next Steps

- [ ] Test route quality against known UK hiking routes (e.g., West Highland Way segments)
- [ ] Evaluate SRTM vs OS Terrain 50 elevation accuracy for hill climb calculation
- [ ] Load test: concurrent routing requests, response times
- [ ] Compare `foot` vs `hike` profile behavior on real-world routes
- [ ] Integration: Design API wrapper for Waymarked backend (authentication, rate limiting, custom response format)
- [ ] Production deployment: Kubernetes/ECS, horizontal scaling, health checks

## Troubleshooting

**Container won't start:**
```bash
docker-compose logs graphhopper
```

**Check graph build progress:**
```bash
docker-compose exec graphhopper ls -lh /data/graph-cache/
```

**Reset everything:**
```bash
docker-compose down -v
rm -rf data/graph-cache data/elevation-cache
docker-compose up -d
```

**API not responding:**
- Wait for graph build to complete (check logs)
- Verify container is healthy: `docker-compose ps`
- Test health endpoint: `curl http://localhost:8989/health`
