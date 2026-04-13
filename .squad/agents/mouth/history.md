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

### Founding Decisions (2026-12-04)

The frontend was bootstrapped as an ASP.NET Core web app with YARP reverse proxy, serving vanilla HTML/CSS/JS with Leaflet 1.9.4 from CDN. No build step. Stack includes:
- **Web Stack:** Leaflet map (UK-focused), sidebar form for lat/lon inputs, stats panel with distance/time/steps
- **Design:** Mobile-first, clean hierarchy, earth-toned palette (`--clr-earth: #2d5a27`)
- **API Integration:** YARP proxies `/api/*` to `waymarked-api` service via Aspire service discovery
- **Initial Tile Layer:** OpenTopoMap (raster, UK topo detail)
- **Route Display:** Stacked polylines — dark outline (#1a0800) + coloured fill (initially orange, then magenta #E0007A)

### Profile & UX Enhancements (2026-12-04)

- Profile dropdown displays dynamic hint text (foot/hike descriptions)
- Collapsible steps list in stats panel shows turn-by-turn instructions (numbered, with distances)
- Route polyline initially blue → orange (for contrast vs water) → magenta (final, avoids road conflicts)

### Dark Mode Implementation (2026-12-04 → 2026-04-13)

Implemented dark mode via CSS tokens + localStorage + OS preference detection. Toggle button (🌙/☀️) in header. Initial tile approach was CSS filter on `.leaflet-tile-pane` (cheap but produced urban artefacts). Later **resolved in v2** by swapping tile provider on theme change: CartoDB Dark Matter (dark) + OpenTopoMap (light).

**Known gotchas:** Button text legibility (fixed via hardcoded white text), header title contrast (fixed via !important rule).

---

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

**Decision:** Fixed button text legibility in dark mode by forcing white text on all buttons

**Key Files:**
- `src/Waymarked.Web/wwwroot/css/app.css` — Added two rules under `[data-theme="dark"]` hardcoded fixes

**Problem:** Two buttons were nearly invisible in dark mode:
- **"Plan Route" button:** Dark grey text (`--clr-white` remapped to `#2a2a2e`) on dark green background (`#2d5a27`)
- **"Show Steps" button (`.steps-toggle`):** Dark green text (`#2d5a27`) on very dark green background (`#1a2d18`)

**Root Cause:** The dark mode token override for `--clr-white` was designed to darken card/panel surfaces, but the global `button` rule used `color: var(--clr-white)`, so buttons inherited the near-black value.

**Fix Applied:**
```css
/* Force white text on all buttons in dark mode */
[data-theme="dark"] button {
    color: #ffffff;
}

/* Steps toggle: match main button appearance */
[data-theme="dark"] .steps-toggle {
    background: var(--clr-earth);
    color: #ffffff;
    border-color: var(--clr-earth-dk);
}
```

**Result:** Both buttons now show white text on brand green (`--clr-earth: #2d5a27`) background in dark mode — readable, clearly clickable, consistent with light mode.

**Commit:** 559b7e6

---

### Dark Mode Map Tiles — Layer Swap (v2) (2026-04-13)

**Decision:** Replaced CSS filter approach with genuine tile provider swap — CartoDB Dark Matter in dark mode, OpenTopoMap in light mode

**Key Files:**
- `src/Waymarked.Web/wwwroot/js/map.js` — Exposed `window.map` and `window.tileLayer` globals for `theme.js` to access
- `src/Waymarked.Web/wwwroot/js/theme.js` — Added `TILE_CONFIG` object with URLs and attribution for both themes; added `swapTileLayer(theme)` function
- `src/Waymarked.Web/wwwroot/css/app.css` — Removed the old filter rule

**Problem:** The previous CSS filter approach (`invert(100%) hue-rotate(180deg)`) on `.leaflet-tile-pane` produced artefacts: warm-grey building tones were only partially reversed, leaving dense urban areas looking light despite overall darkening. The result felt false.

**Solution:** Swap the entire tile layer on theme change instead of filtering a single layer. CartoDB Dark Matter provides a genuine dark map. Layer swap fires on every theme change (toggle, initial load, OS preference detection).

**Implementation Details:**
- `map.js` assigns tile layer and map to `window` globals before `theme.js` loads
- `theme.js` maintains `TILE_CONFIG` with URLs and attribution strings for both themes
- `swapTileLayer(theme)` removes old layer, creates new one, adds to map, calls `bringToBack()` to preserve overlay stacking
- All theme application paths flow through `applyTheme()` (toggle click, OS preference, initial load)

**Tile Layers Used:**

| Theme | Provider | URL |
|-------|----------|-----|
| Light | OpenTopoMap | `https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png` |
| Dark  | CartoDB Dark Matter | `https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png` |

**Constraints Honoured:**
- Route polylines (`#E0007A` magenta + dark outline) are in `.leaflet-overlay-pane` (SVG). `bringToBack()` pushes only the tile layer behind overlays — polylines untouched.
- Script load order: `map.js` → `theme.js` → tile layer swap works on every call
- Vanilla JS, no build step, no framework dependencies

**Note:** This approach supersedes the earlier CSS filter decision. It also **resolves the earlier limitation** that "OpenTopoMap raster tiles were initially undarkened in dark mode." By using CartoDB Dark Matter in dark mode, the issue is eliminated.

**Commit:** (tiles-work branch)

---

### Dark Mode Map Tiles — Version 2: Layer Swap (2026-04-13)

**Decision:** Replace CSS filter approach with genuine tile layer swapping. Use CartoDB Dark Matter in dark mode, keep OpenTopoMap in light mode.

**Key Files:**
- `src/Waymarked.Web/wwwroot/js/map.js` — Exposed `window.tileLayer` and `window.map` globals
- `src/Waymarked.Web/wwwroot/js/theme.js` — Added `TILE_CONFIG` and `swapTileLayer()` function
- `src/Waymarked.Web/wwwroot/css/app.css` — Removed obsolete filter rule

**Problem with v1 (CSS filter):** Built-up urban areas (dense city blocks) remained light-coloured because the hue-rotate partially reversed the inversion on warm-grey building tones. The result didn't feel genuinely dark.

**Solution:** On every theme change (toggle click, initial load, OS preference), swap the entire tile layer via `swapTileLayer(theme)`. The function removes the current layer, creates a new `L.tileLayer` with the appropriate provider, adds it to the map, and calls `bringToBack()` to ensure SVG overlays (route polylines, markers) remain on top.

**Implementation Details:**
- Light mode uses OpenTopoMap (topographic UK tiles)
- Dark mode uses CartoDB Dark Matter (genuinely dark, high contrast)
- Script load order: `map.js` runs before `theme.js`; globals are set before swapping begins
- Route polyline (#E0007A magenta) unaffected — lives in `.leaflet-overlay-pane` (SVG) which stays on top of tile layer

**Result:** Tiles now render with genuine dark appearance in dark mode. No filter artifacts. Map remains readable and branded.

**Commit:** 66172a5

---

### Button Text Colour Fix — Dark Mode (2026-04-13)

**Decision:** Fix button text legibility in dark mode by explicitly pinning button text to white

**Key Files:**
- `src/Waymarked.Web/wwwroot/css/app.css` — Added two hardcoded-colour rules under `[data-theme="dark"]`

**Problem:** 
- **"Plan Route" button:** The global `button { color: var(--clr-white) }` reused the dark-mode token `--clr-white: #2a2a2e` (near-black, meant for panel backgrounds). Result: dark grey text on dark green button background — effectively invisible.
- **"Show Steps" button:** Background was `var(--clr-earth-lt): #1a2d18` (very dark green), text was `var(--clr-earth): #2d5a27` (medium green). Green on dark green — invisible.

**Root Cause:** Token remapping for surfaces unintentionally broke button legibility because buttons reused the same token for text.

**Solution:** Explicitly set button text to pure white (`#ffffff`) in dark mode:
```css
[data-theme="dark"] button {
    color: #ffffff;
}

[data-theme="dark"] .steps-toggle {
    background: var(--clr-earth);        /* #2d5a27 */
    color: #ffffff;
    border-color: var(--clr-earth-dk);
}
```

Both buttons now show white text on brand green background — readable, clearly interactive, consistent with light mode. No light mode impact.

**Commit:** 559b7e6

---

### Fix Export Button Hover Text — Dark Mode (2025-01-27)

**Decision:** Pinned export button (GPX, KML, GeoJSON) hover text to `#ffffff` in dark mode

**Key Files:**
- `src/Waymarked.Web/wwwroot/css/app.css` — Added `[data-theme="dark"] .export-btn:hover` rule

**Problem:** Export buttons showed near-black text on hover in dark mode. The `.export-btn:hover` rule sets `color: var(--clr-white)`, which in dark mode resolves to `#2a2a2e` (near-black panel surface colour). Result: near-black text on dark-green (#2d5a27) hover background — effectively invisible.

**Why the existing fix didn't catch it:** The earlier `[data-theme="dark"] button { color: #ffffff; }` rule sets base button text to white. However, `.export-btn:hover` explicitly declares `color: var(--clr-white)` which appears later in the cascade and wins, overriding the dark mode base fix.

**Fix Applied:**
```css
[data-theme="dark"] .export-btn:hover {
    color: #ffffff;
}
```

Selector: `[data-theme="dark"] .export-btn:hover`. Minimal — only overrides `color` on the broken state. Background (`var(--clr-earth)`) is unaffected.

---

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

