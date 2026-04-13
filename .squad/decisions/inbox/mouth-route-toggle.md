# Decision: Replace disabled-field UX with Route Type toggle

**Date:** 2026-04-13  
**Owner:** Mouth (Frontend Dev)  
**Status:** IMPLEMENTED

## Problem

The form showed both an End Point field and a Distance field simultaneously, with JS disabling one when the other was in use. Users encountered a greyed-out field with no clear explanation — it looked broken rather than intentional.

## Decision

Replace the mutual-exclusion disabled-field pattern with an explicit **Route Type toggle** (segmented pill control) positioned immediately after the Start Point section.

- **Round Trip** (default): shows the Distance field, hides End Point entirely.  
- **Point to Point**: shows the End Point search, hides Distance entirely.

## Rationale

- Showing only the relevant fields eliminates confusion entirely — users can't accidentally interact with a field that doesn't apply to their chosen mode.
- A segmented toggle is a standard mobile UX pattern for mutually exclusive modes; users recognise it immediately.
- The toggle doubles as a clear label for the route type — it communicates the *concept*, not just the mechanical consequence (one field hidden, one shown).

## Implementation

### Files changed
- `src/Waymarked.Web/wwwroot/index.html` — Added `.route-type-toggle` div with `#routeTypeRoundTrip` and `#routeTypePtoP` buttons; wrapped distance field in `#roundTripSection`; wrapped end point field in `#pointToPointSection` (hidden by default); removed "(optional)" span, "Leave blank for round trip" helper, "Distance (round trip only)" label, and the `<hr>` between end point and distance sections.
- `src/Waymarked.Web/wwwroot/js/route.js` — Removed `updateFieldStates()` and its event listener; added `selectRoundTrip()` / `selectPointToPoint()` toggle logic; switching to Round Trip clears end-point hidden inputs so stale coords don't contaminate round-trip API requests.
- `src/Waymarked.Web/wwwroot/css/app.css` — Added `.route-type-toggle` and `.route-type-btn` styles: full-width pill/segmented control, active state uses brand green (`var(--clr-earth)`) + white text, dark mode overrides follow existing token pattern.

### Constraints respected
- Hidden input IDs (`startLat`, `startLon`, `endLat`, `endLon`) unchanged — API contract preserved.
- Form submit logic unchanged — still reads `endLat`/`endLon` emptiness to decide round trip vs. point-to-point.
- Map-click "Set End" mode still works when Point to Point is selected.
- Toggle buttons are `min-height: 44px` — meets mobile HIG minimum touch target.
