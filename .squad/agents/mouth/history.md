# Mouth — Project History

## Project Context

- **Project:** Waymarked — UK walking, hiking, and running route planner
- **Requested by:** Josh Hills
- **Goal:** Build a route planner app from scratch, primarily for UK use. Customisable options, map view of routes. Starting point: find suitable data source(s).
- **Stack:** TBD — no tech decisions made yet.
- **Status:** Team hired, work beginning.

## Key Files

_(none yet — project just started)_

## Learnings

### Waymarked.Web Frontend Created (2026-12-04)

**Decision:** Built frontend as ASP.NET Core web app with YARP reverse proxy and static file serving

**Key Files:**
- `src/Waymarked.Web/Waymarked.Web.csproj` — Web project with YARP 2.2.0
- `src/Waymarked.Web/Program.cs` — YARP proxy config + static files
- `src/Waymarked.Web/appsettings.json` — YARP routing rules (`/api/{**catch-all}` → waymarked-api)
- `src/Waymarked.Web/wwwroot/index.html` — Single-page Leaflet app

**Frontend Stack:**
- No build step — plain HTML/CSS/JS for simplicity
- Leaflet 1.9.4 from CDN for map rendering
- OpenTopoMap tiles (UK-focused topo layer)
- Clean, mobile-first design with sidebar form and full-height map

**Map UX:**
- Defaults to UK view (54.0, -2.0, zoom 6)
- Lat/lon inputs for start/end points (no geocoding yet — that's next)
- End point optional — defaults to start for round trips
- Route displayed as blue polyline with auto-fit bounds
- Stats panel shows distance (km/mi), time estimate, instruction count

**API Integration:**
- YARP proxies `/api/*` to `waymarked-api` service (via Aspire service discovery)
- Avoids CORS issues by keeping API calls on same origin
- Frontend makes relative `/api/routes` POST with `{from, to, profile}`

**Aspire Integration:**
- Added web project to AppHost with `.WithReference(api).WaitFor(api)`
- Added ProjectReference in AppHost.csproj
- Used `AddServiceDefaults()` for observability/health checks
