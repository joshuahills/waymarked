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

### Comment Cleanup + Code Quality Pass (2026-04-15)

**Status:** IMPLEMENTED
**Commit:** af77b47

Stripped verbose/restating comments across all frontend files. Applied high editorial standards.

**What was removed:**
- All section banner dividers (`// ── ...`) from every JS file — they added noise without value in short focused files
- Restating comments (`// Validate start point`, `// Pan/zoom to user location`, `// Show login form with a success hint`, etc.)
- Redundant HTML comments (`<!-- Login form -->`, `<!-- Auth modal -->`, `<!-- Map-click mode toggle -->`, etc.)
- Orphaned/stacked CSS section banners (Map-click mode banner had no styles beneath it)
- `/* Custom select arrow */`, `/* Error banner */`, `/* Crosshair cursor on map... */` CSS comments that restate selectors

**What was kept:**
- FOUC prevention comment in theme.js and index.html `<head>`
- Nominatim mousedown/blur timing explanation in geocoder.js
- Magenta route colour rationale in route.js (hue gap explanation)
- `// Always show success regardless of whether the email exists` in forgot-password (security intent)
- `/* best-effort — clear UI regardless */` in sign-out
- URL token cleanup comment in initPasswordReset
- Dark mode WHY comments tightened but retained

**Quality fix:** Removed 7 dead calls to `updateFieldStates()` in geocoder.js. The function was deleted from route.js during the Route Type Toggle rewrite but the callers in geocoder.js (setStartPoint, clearStartPoint, setEndPoint, clearEndPoint dragend, setupSearch input handler) were never cleaned up. These were silent ReferenceErrors in event handlers.



Added a live password requirements checklist to the register form, plus submit button gating.

**What changed:**
- `index.html`: Added `<ul class="pw-requirements" id="pwRequirements">` inside the password `.auth-form-group`, with 5 `<li class="pw-req" data-req="...">` items (length, uppercase, lowercase, number, special)
- `auth.js`:
  - New DOM refs: `pwRequirements`, `registerPwInput`, `registerCfmInput`, `registerSubmit`
  - `evaluatePassword()` — runs on `input` on both password fields; toggles `.met`/`.unmet` classes per requirement; disables submit unless all 5 pass **and** confirm matches
  - `resetPasswordChecklist()` — removes all `.met`/`.unmet` classes, re-disables submit; called in `closeModal()`
  - Submit button starts `disabled = true` on module init
  - Register `finally` block calls `evaluatePassword()` instead of hard-setting `disabled = false` (keeps state consistent after API errors)
  - API error handler updated: reads `body.errors` array (joins with space) before falling back to `body.message`
- `app.css`:
  - `.pw-requirements` / `.pw-req` / `.pw-req.met` — 0.8rem, muted by default, green (#2d8a3e) when met
  - `::before` pseudo-element: `✗` unmet → `✓` met, with `color` transition
  - Dark mode: met items use `#7ab87a` (matches existing auth switch link colour)

**Requirements checked (ASP.NET Core Identity defaults):**
- At least 6 characters
- At least one uppercase letter (A–Z)
- At least one lowercase letter (a–z)
- At least one number (0–9)
- At least one non-alphanumeric (special) character

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

---

### Remove Emoji Icons from UI (2026-04-13)

**Status:** IMPLEMENTED  
**Decision:** .squad/decisions/inbox/mouth-no-emoji-icons.md

Removed all emoji icons from the UI, replacing them with inline SVGs or plain text labels as appropriate.

**What changed:**
- Theme toggle button: Replaced 🌙/☀️ emojis with inline SVG moon/sun icons (16×16). Updated `theme.js` to swap icons via `innerHTML` instead of `textContent`.
- Map mode toggles: Removed 📍 emojis from "Set Start" and "Set End" buttons — text labels are sufficient.
- Route type toggle: Removed 🔄 emoji from "Round Trip" button — text label is sufficient.
- Stats section: Removed all emoji spans (📏, ⏱, 🗺) — plain text labels ("Distance", "Estimated Time", "Instructions") are clearer.

**Unicode characters preserved:** ▾/▴ (steps toggle), ✕ (Off button), → (Point to Point) — these are geometric/semantic characters, not emojis, with consistent cross-platform rendering.

**Key files changed:**
- index.html: SVG moon icon in theme toggle button, removed emojis from map mode buttons, route type button, and all stat labels
- theme.js: `applyTheme()` now uses `innerHTML` to swap between moon and sun SVG icons

**Rationale:** Emojis create inconsistent rendering across platforms, accessibility issues, and poor visual control. SVG icons and plain text provide better consistency, accessibility, and design system alignment.

---

### Auth UI — Login/Register Modal (2026-04-14)

**Status:** IMPLEMENTED
**Decision:** .squad/decisions/inbox/mouth-auth-ui.md

Built the frontend auth UI against ASP.NET Core Identity backend. Cookie auth — JS doesn't handle tokens.

**Key files changed/created:**
- `src/Waymarked.Web/wwwroot/index.html` — `.header-auth` nav area + `#authModal` with login/register forms
- `src/Waymarked.Web/wwwroot/css/app.css` — auth styles appended; moved `margin-left: auto` from `.theme-toggle` to `.header-auth`
- `src/Waymarked.Web/wwwroot/js/auth.js` — new file; all auth logic

**Architecture decisions:**
- Modal overlay (not a separate page or sliding panel) — keeps user in context, map stays visible
- Login and Register share one modal, toggled by switching form visibility
- On load: `GET /api/auth/me` silently checks session; failure = logged-out state, no alert
- Sign-out is best-effort: clears UI even if the request fails
- Auth doesn't block the map — "Sign in" button is opt-in from the header

**Dark mode:** Follows existing patterns — `[data-theme="dark"] button { color: #ffffff }` covers modal submit buttons. Close button gets explicit `color: #ffffff` on hover override. Switch links use `#7ab87a` (green tint) in dark mode.

**Mobile:** Modal constrained with `margin: 0.5rem` on small screens. Header email truncated/hidden at <768px to save space. All interactive elements meet 44×44px minimum touch target.

**Dark mode gotcha:** `.header-auth-signin` and `.header-auth-signout` sit on the always-green header, so they must always be `color: #ffffff`. The `[data-theme="dark"] button { color: #ffffff }` global already handles this, but I've added explicit overrides to document intent and prevent any future specificity regression.

## Learnings

- **`margin-left: auto` in flex headers:** Only one element should own it to define the right-push. When adding new right-side controls, transfer `margin-left: auto` to the outermost grouping element.
- **Cookie auth simplicity:** With HttpOnly cookies + YARP, the frontend auth module is just UI state — no token storage, no refresh logic. `fetch(..., { credentials: 'same-origin' })` is all that's needed.
- **Sign-out best-effort pattern:** Always update UI after sign-out regardless of whether the API call succeeds. Users expect the UI to reflect their intent, not the network.
- **Modal `hidden` attribute:** Prefer `hidden` attribute + `[hidden] { display: none }` CSS over toggling `display` directly — more semantic, works with forms for reset state.
- **`display: flex` beats `[hidden]`:** Any element with an explicit `display` CSS rule will override the browser's built-in `[hidden] { display: none }`. Always add `[selector][hidden] { display: none; }` alongside any flex/grid container that is also toggled via the `hidden` attribute.
- **Clear text on hide:** When hiding a container that holds user data (e.g. an email span), always clear the text content too — hiding the wrapper alone leaves stale data in the DOM that can flash if the element becomes visible again.
- **Guard auth probes with a localStorage flag:** With cookie auth, JS can't inspect the cookie directly. A `localStorage` flag (`waymarked_authed`) set on login/register and cleared on logout lets us skip the `/me` round-trip for anonymous users entirely. If the cookie expires, the 401 self-heals by clearing the flag — no stale state risk.
- **`setLoggedOut()` must own the flag clear:** Centralising `localStorage.removeItem('waymarked_authed')` inside `setLoggedOut()` means logout, 401 self-heal, and any future code paths all stay in sync automatically.


---

### Password Requirements Checklist — Register Form (2026-04-14)

**Status:** IMPLEMENTED

Added a live password requirements checklist to the register form, plus submit button gating.

**What changed:**
- `index.html`: Added `<ul class="pw-requirements" id="pwRequirements">` inside the password `.auth-form-group`, with 5 `<li class="pw-req" data-req="...">` items (length, uppercase, lowercase, number, special)
- `auth.js`:
  - New DOM refs: `pwRequirements`, `registerPwInput`, `registerCfmInput`, `registerSubmit`
  - `evaluatePassword()` — runs on `input` on both password fields; toggles `.met`/`.unmet` classes per requirement; disables submit unless all 5 pass **and** confirm matches
  - `resetPasswordChecklist()` — removes all `.met`/`.unmet` classes, re-disables submit; called in `closeModal()`
  - Submit button starts `disabled = true` on module init
  - Register `finally` block calls `evaluatePassword()` instead of hard-setting `disabled = false` (keeps state consistent after API errors)
  - API error handler updated: reads `body.errors` array (joins with space) before falling back to `body.message`
- `app.css`:
  - `.pw-requirements` / `.pw-req` / `.pw-req.met` — 0.8rem, muted by default, green (#2d8a3e) when met
  - `::before` pseudo-element: `✗` unmet → `✓` met, with `color` transition
  - Dark mode: met items use `#7ab87a` (matches existing auth switch link colour)

**Requirements checked (ASP.NET Core Identity defaults):**
- At least 6 characters
- At least one uppercase letter (A–Z)
- At least one lowercase letter (a–z)
- At least one number (0–9)
- At least one non-alphanumeric (special) character

### Code Quality Review Findings + Frontend Cleanup (2026-04-14)

**Status:** COMPLETED

**Mikey's code quality review findings:**
- **🐛 CRITICAL BUG:** `updateFieldStates()` called 7 times in geocoder.js but never defined
- Function was deleted during Route Type Toggle rewrite but callers remained as silent ReferenceErrors

**Fix applied:**
- Removed all 7 dead calls to `updateFieldStates()` in geocoder.js (lines 47, 50, 57, 82, 85, 93, 156)
  - setStartPoint (line 47)
  - clearStartPoint (line 50)
  - setEndPoint (line 57)
  - clearEndPoint (line ?)
  - dragend marker handler (lines 82, 85, 93)
  - setupSearch input handler (line 156)

**Comment cleanup pass:**
- Removed: 103 lines of verbose/restating comments across 10 frontend files
- Added: 13 lines of essential comments (security rationale, dark mode WHY, design intent)
- Kept: FOUC prevention, Nominatim timing explanation, route colour rationale, security intent comments
- Pattern enforced: Remove section banners, restatement comments, redundant HTML comments; keep WHY-based explanations

**Commit:** af77b47 (comment cleanup + dead call removal)
**Result:** Runtime crash bugs eliminated; code health improved.


