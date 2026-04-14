# Mikey — Project History

## Project Context

- **Project:** Waymarked — UK walking, hiking, and running route planner
- **Requested by:** Josh Hills
- **Goal:** Build a route planner app from scratch, primarily for UK use. Customisable options, map view of routes. Starting point: find suitable data source(s).
- **Stack:** TBD — no tech decisions made yet. Data source research is the first priority.
- **Status:** Team hired, work beginning.

## Key Files

_(none yet — project just started)_

## Learnings

### 2026-04-12: Routing Engine Research

**Evaluated routing engines for UK walking/hiking/running:**

**Valhalla (RECOMMENDED):**
- MIT license (permissive, commercial-friendly)
- Excellent pedestrian/hiking profiles with elevation support built-in (Skadi module)
- Uses OSM data — UK has exceptional OSM footpath coverage
- Self-hostable via Docker, low resource requirements for UK data (~1.5GB OSM extract, 4-8GB RAM)
- Active development, production-ready (used by Mapbox, Stadia Maps)
- Node.js and Python bindings available
- Built-in: isochrones, map-matching, elevation sampling, turn-by-turn
- UK + Ireland graph builds in minutes, millisecond route responses

**GraphHopper (FALLBACK):**
- Apache 2.0 license (permissive)
- Excellent custom model support, has built-in `hike.json` profile
- Strong round-trip route generation (Valhalla lacks this natively)
- Java-based (heavier stack than Node.js)
- API pricing escalates quickly for commercial use
- Good self-hosting option but less documented than Valhalla

**OpenRouteService (REJECTED):**
- GPL-3.0 license (copyleft — forces open-sourcing modifications, dealbreaker for commercial)
- Fork of GraphHopper, Java/Spring Boot stack
- Excellent API for free public use, but licensing blocks commercial self-hosting
- Built for humanitarian/academic use

**OSRM (NOT SUITABLE):**
- BSD-like license (permissive)
- Car-focused routing, basic pedestrian profiles
- No built-in elevation support
- Contraction Hierarchies make runtime customization difficult
- Wrong tool for walking/hiking use case

**Key Technical Decisions:**
- Backend: Node.js + Express (aligns with Valhalla Node.js bindings, faster iteration)
- Frontend: React + Mapbox GL JS (best performance for vector tiles, free tier sufficient initially)
- Mapping data: OSM via Mapbox vector tiles or self-hosted OpenMapTiles
- Deployment: Docker-based Valhalla on single VPS initially, scale horizontally as needed

**Critical Path Dependencies:**
- Data (GIS Engineer) must validate UK OSM footpath coverage, surface tag prevalence, elevation data accuracy
- Need to confirm Public Rights of Way, coastal paths, and National Trails are properly tagged
- Elevation: SRTM 90m may need upgrade to OS Terrain 50 for accuracy (licensing/cost TBD)
- Round-trip routing: Valhalla lacks native support — may need custom algorithm or GraphHopper integration

**Risks Identified:**
- OSM data quality for UK footpaths — requires spot-checking (Data team)
- Round-trip route generation — Valhalla doesn't support, GraphHopper does (potential hybrid approach)
- Multi-day route planning with accommodation — needs custom logic
- Weekly OSM updates require blue/green deployment to avoid downtime

**Next Actions:**
- Data validates UK OSM coverage within 1 week
- Spike: Deploy Valhalla with UK extract, test pedestrian routing + custom costing
- Spike: Test "avoid roads", "prefer scenic", elevation-based routing
- Decision: If OSM insufficient, evaluate contributing to OSM vs. hybrid OS MasterMap approach

### 2026-04-13: Auth Implementation Code Review

**Files Reviewed:**
- `src/Waymarked.Api/AuthEndpoints.cs` — registration, login, logout, /me, forgot-password, reset-password
- `src/Waymarked.Api/Email/SmtpEmailSender.cs` — MailKit-based email sender
- `src/Waymarked.Api/Email/SmtpSettings.cs` — SMTP configuration POCO
- `src/Waymarked.Api/Program.cs` — Identity and cookie configuration
- `src/Waymarked.Api.Tests/AuthEndpointTests.cs` — integration tests (register, login, logout, /me)
- `src/Waymarked.E2E.Tests/AuthJourneyTests.cs` — Playwright E2E tests covering full auth UI flows

**Security Findings:**

✅ **Done Well:**
- Cookie security correctly configured: HttpOnly=true, SameSite=Strict, SecurePolicy=Always
- `/api/auth/me` returns 401 (not redirect) for unauthenticated requests — correct API behaviour
- Password reset endpoint uses constant-time response (always 200) to prevent email enumeration
- Password reset tokens are URL-encoded via `HttpUtility.UrlEncode()`
- SMTP credentials loaded from configuration (not hardcoded)
- ASP.NET Core Identity defaults used for password complexity

⚠️ **Concerns:**
1. **lockoutOnFailure: false** — No brute-force protection on login. Intentional for MVP, but needs addressing before production.
2. **No CSRF protection** — State-mutating POST endpoints (login, logout, register) use cookie auth but no anti-forgery tokens. SameSite=Strict mitigates most CSRF in modern browsers, but older browsers vulnerable.
3. **Token lifespan not explicitly configured** — Password reset tokens default to 1 day, but this should be made explicit and documented.
4. **No rate limiting** — Forgot-password endpoint could be abused for email spam.

**Maintainability Findings:**

✅ **Done Well:**
- Endpoint organisation is clean and cohesive
- Error responses use consistent `{ errors: string[] }` shape
- Request records are minimal and appropriate
- Separation between endpoint logic and email infrastructure is good
- Test coverage for core flows is solid (28+ integration tests, 17+ E2E tests)

⚠️ **Concerns:**
1. **Missing API-level tests for password reset endpoints** — Only E2E tests cover forgot/reset flows; no direct HTTP integration tests.
2. **Email template strings inline** — HTML templates embedded in SmtpEmailSender; extract for maintainability when design stabilises.

**Verdict: APPROVED WITH NOTES** — Implementation is solid for MVP phase. Security fundamentals correct. Pre-production requires: lockout policy, explicit token lifespan, and rate limiting.

### 2026-04-14: Full Codebase Code Quality Review

**Status:** COMPLETED

**Scope:** All backend C#, test, and frontend JS/HTML files.

**Critical Bug Found:**
- `updateFieldStates()` is called 7 times in `geocoder.js` but never defined anywhere — runtime ReferenceError on any marker/search interaction. (Mouth fixed this.)

**Architecture Observations:**
- Email infrastructure is clean. `SendConfirmationLinkAsync` and `SendPasswordResetCodeAsync` are interface-required (`IEmailSender<T>`) but never called by app code. Correct and harmless.
- Export endpoints in `Program.cs` (GPX/KML/GeoJSON) duplicate the validate→build→execute pipeline. DRY violation — extract shared `GetRouteOrError()` method.
- Frontend uses global `window.map` / `window.tileLayer` for cross-file communication. Acceptable for vanilla JS architecture, would need rethinking if modules are adopted.

**Template Boilerplate to Clean (Brand completed):**
- `Program.cs:15` — `// Add services to the container.`
- `Program.cs:78` — `// Learn more about configuring OpenAPI...`
- `AuthWebApplicationFactory.cs` — 4x `// No-op for tests.` comments on self-evident empty methods.

**What's Good:**
- Auth test structure is excellent — clear naming, good coverage, useful doc comments.
- Frontend JS is well-organised for vanilla architecture — IIFEs, debounce, clean event wiring.
- Security comments (anti-enumeration rationale, etc.) are valuable and should stay.

**Decision:** Written to `.squad/decisions/inbox/mikey-code-quality-review-20260414.md`
