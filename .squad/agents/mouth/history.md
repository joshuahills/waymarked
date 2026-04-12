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

### Collapsible Route Steps List (2026-12-04)

**Decision:** Added collapsible steps toggle to stats panel for turn-by-turn route instructions

**Key Files:**
- `src/Waymarked.Web/wwwroot/index.html` — Added steps button and list container to stats panel
- `src/Waymarked.Web/wwwroot/js/map.js` — Added DOM refs for stepsToggle and stepsList
- `src/Waymarked.Web/wwwroot/js/route.js` — Added showSteps function, toggle handler, integrated with route display
- `src/Waymarked.Web/wwwroot/css/app.css` — Added styling for toggle button and numbered steps list

**UX Pattern:**
- Toggle button hidden by default (only shows when route is loaded)
- Starts collapsed ("▾ Show steps")
- Filters out finish instruction (sign === 4)
- Shows numbered instructions with inline distances
- Distance formatting: <100m shows metres, ≥100m shows km with one decimal
- Clean visual hierarchy: step number in earth-green, distance in muted grey, right-aligned

**Implementation Details:**
- hideStats() now clears steps state (toggle hidden, list empty, display none)
- showSteps() called immediately after showStats() with data.instructions
- Toggle switches between "▾ Show steps" and "▴ Hide steps"
- CSS uses flexbox for step layout with auto-margin for distance alignment
- Overrides global button::after arrow with empty content for toggle button

