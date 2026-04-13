# Mouth — Project History

## Project Context

- **Project:** Waymarked — UK walking, hiking, and running route planner
- **Requested by:** Josh Hills
- **Goal:** Build a route planner app from scratch, primarily for UK use. Customisable options, map view of routes. Starting point: find suitable data source(s).
- **Stack:** TBD — no tech decisions made yet.
- **Status:** Team hired, work beginning.

## Key Files

_(none yet — project just started)_

## Core Context

### Founding & Initial Implementation Summary (2026-12-04 → 2026-04-12)

**Frontend Stack:** ASP.NET Core web app with YARP reverse proxy, vanilla HTML/CSS/JS (no build step), Leaflet 1.9.4 map library from CDN.

**Architecture:** Mobile-first UI with sidebar form (lat/lon inputs), stats panel (distance/time/steps), full-height map. Route display uses stacked polylines (dark outline #1a0800 + magenta fill #E0007A). OpenTopoMap tiles (raster, UK topographic detail). Profile dropdown with dynamic hint text. Collapsible steps list shows turn-by-turn directions.

**Design System:** Earth-toned palette (`--clr-earth: #2d5a27`, `--clr-earth-dk: #1a2d18`). CSS tokens + localStorage for theming + OS preference detection. Dark mode implemented with dual tile layer swap (CartoDB Dark Matter dark, OpenTopoMap light). Known pattern: any pseudo-state element (`:hover`, `:focus`) setting `color: var(--clr-white)` needs explicit dark mode override to `#ffffff`.

**API Integration:** YARP proxies `/api/*` to `waymarked-api` service via Aspire service discovery. Post endpoints with route requests (A→B and round-trip support via optional destination).

**Dark Mode Learnings:** Token remapping for surfaces conflicts with text colour unless overridden. Button text required hardcoded white (#ffffff) in dark mode (commit 559b7e6). Export buttons required additional hover state override (commit b5a70be). Pattern resolved: always use explicit colours in interactive pseudo-states rather than remapped tokens.

---

## Recent Work

### Geolocation — "Use My Location" (2026-04-13)

**Status:** IMPLEMENTED  
**Commit:** (this work)

Added "Use my location" (crosshair icon) button next to the Start Point field.

**Key Files:**
- `src/Waymarked.Web/wwwroot/index.html` — `.search-with-geoloc` wrapper, `#geolocBtn`, `#geolocError`
- `src/Waymarked.Web/wwwroot/js/geolocation.js` — New file; all geolocation logic
- `src/Waymarked.Web/wwwroot/css/app.css` — Button, error, and "you are here" marker styles

**What it does:**
- Calls `navigator.geolocation.getCurrentPosition()` on click
- On success: reverse-geocodes via Nominatim, calls `setStartPoint()`, pans map to zoom 14, places pulsing blue "you are here" dot
- On error: shows friendly inline error with `role="alert"` for a11y
- On unsupported browsers: hides button entirely

**Dark mode gotcha (same pattern as buttons):** The `.geoloc-btn:hover` pseudo-class has higher specificity than `[data-theme="dark"] button`, so it wins in dark mode and can revert to dark icon colour. Added explicit `[data-theme="dark"] .geoloc-btn:hover { color: #ffffff }` override.

**Touch target:** Button is exactly `44×44px` — meets mobile HIG minimum.

**"You are here" marker:** Blue pulsing dot (`#007AFF`, CSS animation `yah-pulse`) — distinct from route S/E markers (green/red pin icons). Implemented as a Leaflet DivIcon with CSS classes.

**Script load order:** `map.js → geocoder.js → geolocation.js → markers.js → ...` (geolocation.js depends on `reverseGeocode`, `coordLabel`, `setStartPoint` from geocoder.js, and `map` global from map.js)


### Dark Mode Button Styling Fix (2026-04-13)

### Fix Export Button Hover Text — Dark Mode (Continued) (2026-04-13)

**Status:** IMPLEMENTED  
**Commit:** b5a70be

The earlier button text fix (commit 559b7e6) addressed the base state but missed a cascade issue: the `.export-btn:hover` rule explicitly declares `color: var(--clr-white)`, which wins over the base `[data-theme="dark"] button { color: #ffffff; }` rule (equal specificity, later rule in cascade wins). This meant export buttons reverted to near-black text on hover in dark mode.

**Fix:** Added `[data-theme="dark"] .export-btn:hover { color: #ffffff; }` to override the explicit colour on the hover state. Selector specificity now wins, and white text appears correctly on the dark-green hover background.

**Key insight:** Token remapping creates dual-duty conflicts when the same token is used for both surfaces and text. The pattern to watch: any element setting `color: var(--clr-white)` in a pseudo-state (`:hover`, `:focus`, etc.) needs a dark-mode override. Future button-like elements should use explicit `#ffffff` instead of the token in interactive states.

---

### Geolocation — "Use My Location" (2026-04-13)

**Status:** IMPLEMENTED  
**Commit:** e5113c5

Added "Use my location" (crosshair icon) button next to the Start Point field.

**Key Files:**
- `src/Waymarked.Web/wwwroot/index.html` — `.search-with-geoloc` wrapper, `#geolocBtn`, `#geolocError`
- `src/Waymarked.Web/wwwroot/js/geolocation.js` — New file; all geolocation logic
- `src/Waymarked.Web/wwwroot/css/app.css` — Button, error, and "you are here" marker styles

**What it does:**
- Calls `navigator.geolocation.getCurrentPosition()` on click
- On success: reverse-geocodes via Nominatim, calls `setStartPoint()`, pans map to zoom 14, places pulsing blue "you are here" dot
- On error: shows friendly inline error with `role="alert"` for a11y
- On unsupported browsers: hides button entirely

**Dark mode gotcha (same pattern as buttons):** The `.geoloc-btn:hover` pseudo-class needs explicit `color: #ffffff` override in dark mode to prevent specificity issues.

**Touch target:** Button is exactly `44×44px` — meets mobile HIG minimum.

**"You are here" marker:** Blue pulsing dot (`#007AFF`, CSS animation `yah-pulse`) — distinct from route S/E markers (green/red pin icons). Implemented as a Leaflet DivIcon with CSS classes.

**Script load order:** `map.js → geocoder.js → geolocation.js → markers.js → ...` (geolocation.js depends on `reverseGeocode`, `coordLabel`, `setStartPoint` from geocoder.js, and `map` global from map.js)

---

### README Updates (2026-04-13)

**Status:** COMPLETED  
**Commits:** a10e14d (root + src/)

Updated root README.md and src/README.md:
- Renamed ".NET Aspire" to "Aspire" throughout
- Version bump: Aspire 9 → 13
- Enhanced features list: added dark mode, geolocation, export support
- Updated prerequisites: aspire CLI tool
- Updated run command: `aspire run`
- Expanded Projects section with Waymarked.Web + all 3 test projects (Waymarked.Routing.Tests, Waymarked.Api.Tests, [pending])
- Filled Testing section with actual test projects and `dotnet test` instructions
- Updated architecture diagram
- Removed stale squad inbox reference



---

### Route Type Toggle - Replace Disabled-Field UX (2026-04-13)

**Status:** IMPLEMENTED
**Decision:** .squad/decisions/inbox/mouth-route-toggle.md

Replaced the mutual-exclusion disabled-field pattern with a **Route Type toggle** - a segmented pill control after the Start Point.

**Key files changed:**
- index.html: route-type-toggle with #routeTypeRoundTrip / #routeTypePtoP. Distance in #roundTripSection (visible), End Point in #pointToPointSection (hidden). Removed stale helpers/labels/hr.
- route.js: Removed updateFieldStates(). Added selectRoundTrip() / selectPointToPoint(). Switching to Round Trip clears endLat/endLon/endSearch.
- app.css: .route-type-toggle/.route-type-btn styles - full-width pill, var(--clr-earth) active bg + #ffffff text, dark mode overrides.

**Dark mode pattern:** Active and active:hover use hardcoded #ffffff, not var(--clr-white) which remaps to near-black in dark mode.
**Touch target:** min-height 44px on toggle buttons.
