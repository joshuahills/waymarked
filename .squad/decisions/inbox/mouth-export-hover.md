# Decision: Fix Export Button Hover Text in Dark Mode

**Date:** 2025-01-27  
**Author:** Mouth (Frontend Dev)  
**Status:** Implemented

## Bug Cause

The `.export-btn:hover` rule sets `color: var(--clr-white)`. In light mode this resolves to `#ffffff` (white) — correct on the dark green hover background. In dark mode, the token override remaps `--clr-white` to `#2a2a2e` (near-black) to darken panel surfaces. As a result, hover text on export buttons (GPX, KML, GeoJSON) renders as near-black on a dark green (`#2d5a27`) background — effectively invisible.

## Root Cause Pattern

The `--clr-white` token serves dual duty: it's used as both a *surface colour* (panels, card backgrounds) and as *text colour* (contrast on green buttons). The dark mode override correctly darkens surfaces but inadvertently breaks any `color: var(--clr-white)` usage on hover states.

The existing `[data-theme="dark"] button { color: #ffffff; }` rule fixes the base button state but does not override the explicit `color: var(--clr-white)` set in `.export-btn:hover` — specificity is equal, but the hover rule appears later in the cascade and wins.

## Fix

Added a targeted dark mode hover override in `src/Waymarked.Web/wwwroot/css/app.css`:

```css
/* export-btn hover: --clr-white remaps to #2a2a2e in dark mode, making
   hover text near-black on dark-green. Pin to literal white. */
[data-theme="dark"] .export-btn:hover {
    color: #ffffff;
}
```

**Selector used:** `[data-theme="dark"] .export-btn:hover`

The fix is minimal and surgical — it only overrides the one property (`color`) on the one state (`:hover`) that was broken. The background colour (`var(--clr-earth)`) is unaffected and renders correctly in both modes.
