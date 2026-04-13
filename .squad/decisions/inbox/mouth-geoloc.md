# Decision: Add "Use My Location" Geolocation Feature

**Date:** 2026-04-13  
**Status:** IMPLEMENTED  
**Owner:** Mouth (Frontend Dev)

## Summary

Added a "Use my location" button (crosshair icon) next to the Start Point search field. Uses `navigator.geolocation.getCurrentPosition()` to populate the start point and pan the map to the user's location.

## Problem

Users on mobile had to manually type or tap a place name to set their start point. On a trail, this is awkward — you're already standing where you want to begin.

## Solution

Small `44×44px` icon button placed inline with the start point search field. Clicking it:

1. Shows a loading state (spinning crosshair animation)
2. Calls `navigator.geolocation.getCurrentPosition()`
3. On success: reverse-geocodes the coordinate via Nominatim, calls `setStartPoint()` with the result, pans the map to the location at zoom 14, and places a distinct "you are here" pulsing blue dot marker
4. On error: shows a friendly inline error message (e.g., "Location unavailable — please enable location access")
5. Hides itself entirely on browsers without geolocation support

## Key Files

- `src/Waymarked.Web/wwwroot/index.html` — Added `.search-with-geoloc` wrapper, `#geolocBtn`, `#geolocError`
- `src/Waymarked.Web/wwwroot/js/geolocation.js` — New file; geolocation logic
- `src/Waymarked.Web/wwwroot/css/app.css` — `.search-with-geoloc`, `.geoloc-btn`, `.geoloc-error`, `.yah-outer/.yah-inner`, dark mode overrides

## Design Decisions

- **Layout:** Flex row wrapping the existing `.search-wrapper` — no disruption to the existing form structure. Dropdown remains anchored to the input via `search-wrapper`'s `position: relative`.
- **Touch target:** Button is exactly `44×44px` — meets Apple/Google mobile HIG minimum tap target size.
- **"You are here" marker:** Blue pulsing dot (`#007AFF`) — deliberately distinct from route markers (green S, red E). Persists independently of the route start marker so users can see both their GPS location and chosen start point if they diverge.
- **Dark mode:** Button inherits `color: #ffffff` from the existing `[data-theme="dark"] button` rule. Hover state explicitly overridden to `#243320` (dark earth green) with white icon — avoids `--clr-earth-lt` which remaps to very dark green in dark mode.
- **Error UX:** Inline `<p role="alert" aria-live="polite">` element — announced by screen readers, visible below the search row, dark mode aware.
- **Geolocation options:** `timeout: 10000ms`, `maximumAge: 60000ms` — allows cached positions up to 1 minute old (fast on repeat taps), fails cleanly after 10 seconds.

## Dark Mode Cascade Note

The global `[data-theme="dark"] button { color: #ffffff }` rule has specificity `(0,1,0,1)`, which beats `.geoloc-btn { color: var(--clr-earth) }` at `(0,0,1,0)`. This means in dark mode, the icon is white on the dark button background — correct. The only override needed is `.geoloc-btn:hover` (specificity `(0,0,2,0)` — beats the global rule), so the hover state is explicitly pinned to `color: #ffffff` in dark mode.

## Script Load Order

`geolocation.js` loads after `geocoder.js` (uses `reverseGeocode`, `coordLabel`, `setStartPoint`) and `map.js` (uses `map` global). Load order: `map.js → geocoder.js → geolocation.js → markers.js → ...`
