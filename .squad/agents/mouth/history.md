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

### Profile Description Hint (2026-12-04)

**Decision:** Added dynamic profile description below the Route Profile dropdown

**Key Files:**
- `src/Waymarked.Web/wwwroot/index.html` — Added `<small id="profile-description" class="helper-text">` after `<select id="profile">`
- `src/Waymarked.Web/wwwroot/js/map.js` — Added DOM refs: `profileSelect`, `profileDescription`
- `src/Waymarked.Web/wwwroot/js/route.js` — Added `profileDescriptions` map, `updateProfileDescription()` function, wired to `change` event and called on load

**UX Pattern:**
- Reused existing `.helper-text` CSS class (0.78rem, `--clr-muted`) — no new styles needed
- `foot` → "Prefers footpaths and pavements. Good for shorter walks on mixed terrain."
- `hike` → "Prefers trails and bridleways, avoids steep terrain. Best for longer scenic routes."
- Description initialised on page load to match default `foot` selection
- Updates instantly on dropdown change — no animation

---

### Route Polyline Colour — High-Contrast Orange (2026-12-04)

**Decision:** Replaced blue route polyline with vivid orange (#FF6B00) + dark outline for strong contrast against OpenTopoMap water features

**Key Files:**
- `src/Waymarked.Web/wwwroot/js/route.js` — Replaced single `L.polyline` with `L.featureGroup` of two stacked polylines

**Problem:** `#007bff` (blue) blended into OpenTopoMap's blue-grey rivers, streams and brooks, making the route hard to follow through river valleys.

**Solution:** Two stacked polylines via `L.featureGroup`:
- **Outline:** `#1a0800`, weight 7, opacity 0.55 — dark border to lift the line off any tile colour (woodland green, path beige, etc.)
- **Fill:** `#FF6B00`, weight 4, opacity 0.95 — vivid orange that reads clearly against water blue at all zoom levels

**Why orange:** Strong hue contrast against blue (complementary-adjacent on colour wheel). Consistent with existing brand palette (`--clr-trail: #c8590a`). Avoids conflict with green woodland, brown paths, and grey rock tiles on OpenTopoMap.

**Implementation note:** `L.featureGroup` wraps both polylines so `map.removeLayer(routeLayer)` and `routeLayer.getBounds()` continue to work without API changes elsewhere.

---

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

---

### Route Polyline Colour — Magenta (2026-12-04)

**Decision:** Changed route polyline fill from orange (#FF6B00) to magenta (#E0007A)

**Key Files:**
- `src/Waymarked.Web/wwwroot/js/route.js` — updated `color` on the fill polyline

**Problem:** Orange (#FF6B00) conflicts with A roads and motorways on OpenTopoMap, which render in orange/red tones — making the route indistinguishable from road overlays.

**Solution:** Magenta (#E0007A) occupies the hue gap between:
- Blue/cyan (water — rivers, lakes, streams)
- Orange/red/yellow (roads — motorways, A roads, B roads)
- Green (woodland, parks)

Reads clearly at all zoom levels against OpenTopoMap tiles. The dark outline (#1a0800, weight 7) is unchanged.

**Commit:** 24309ba

---

### Dark Mode Toggle (2026-12-04)

**Decision:** Implemented dark mode via CSS custom property overrides on `[data-theme="dark"]` with OS preference detection, localStorage persistence, and a 🌙/☀️ toggle button in the header.

**Key Files:**
- `src/Waymarked.Web/wwwroot/css/app.css` — Added `[data-theme="dark"]` token block + Leaflet control overrides + hardcoded colour fixes
- `src/Waymarked.Web/wwwroot/index.html` — Anti-FOUC inline script in `<head>`, `#theme-toggle` button in `<header>`
- `src/Waymarked.Web/wwwroot/js/theme.js` — Toggle wiring, localStorage persistence, aria-label sync

**Approach:**
- CSS tokens (`--clr-stone`, `--clr-white`, `--clr-ink`, `--clr-muted`, `--clr-border`, `--clr-earth-lt`, `--clr-trail-lt`) redefined under `[data-theme="dark"]` on `<html>` — all tokenised surfaces update automatically
- Anti-FOUC inline script runs before any paint: reads `localStorage.getItem('theme')`, falls back to `matchMedia('prefers-color-scheme: dark')`
- Toggle button lives in the header (right edge via `margin-left:auto`) — uses emoji icons (🌙/☀️) with `aria-label` that flips on each press
- Hardcoded colours that escaped the token system were patched individually: select SVG arrow stroke, error banner, steps-toggle hover, mode-btn.active-off hover

**Leaflet dark overrides:**
- Zoom bar, attribution, popup wrapper/tip all receive dark backgrounds and light text via `.leaflet-*` class overrides scoped to `[data-theme="dark"]`

**Known limitation (resolved):**
- OpenTopoMap raster tiles were initially undarkened in dark mode. This was later solved with a CSS filter on `.leaflet-tile-pane` — see Dark Map Tiles decision below.

**Commit:** 1ae475d

---

### Dark Mode Map Tiles — CSS Filter on Tile Pane (2026-12-04)

**Decision:** Darkened OpenTopoMap raster tiles in dark mode using a CSS filter scoped to `.leaflet-tile-pane`

**Key Files:**
- `src/Waymarked.Web/wwwroot/css/app.css` — added `[data-theme="dark"] .leaflet-tile-pane` rule

**Filter applied:**
```css
filter: invert(100%) hue-rotate(180deg);
```

- `invert(100%)` flips tile luminance so the pale cream/white base map goes dark
- `hue-rotate(180deg)` rotates hues back to prevent the "colour negative" effect — green terrain stays greenish, water stays bluish

**Scope:** Filter targets `.leaflet-tile-pane` only (the raster tile layer). The `.leaflet-overlay-pane` (SVG — route polylines, markers) is intentionally excluded. The magenta route polyline remains unchanged at `#E0007A` in both themes.

**Supersedes:** The "Known limitation" note from the Dark Mode Toggle entry above. Tiles can now be darkened via this approach.

---

### Header Title Colour Fix (2026-04-13)

**Decision:** Pinned header h1 text to white with `!important` to ensure title remains visible in dark mode

**Key Files:**
- `src/Waymarked.Web/wwwroot/css/app.css` — Added `[data-theme="dark"] header h1` rule with explicit `color: white !important`

**Problem:** The header h1 element was inheriting the dark mode text colour (`--clr-ink: #e8e8ea`), which resulted in very light/faded text that was barely visible against the green header background in dark mode.

**Solution:** Explicitly pin h1 colour to pure white (`#ffffff`) within the dark mode scope. The `!important` flag ensures this overrides any cascading rules from the token system.

**Result:** Title "Waymarked" now clearly readable in both light and dark modes.

**Commit:** 6329fe5

---

### Fix Button Dark Mode Styling (2026-04-13)

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

