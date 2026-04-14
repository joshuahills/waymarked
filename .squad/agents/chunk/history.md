# Chunk — Project History

## Project Context

- **Project:** Waymarked — UK walking, hiking, and running route planner
- **Requested by:** Josh Hills
- **Goal:** Build a route planner app from scratch, primarily for UK use. Customisable options, map view of routes. Starting point: find suitable data source(s).
- **Stack:** TBD — no tech decisions made yet.
- **Status:** Team hired, work beginning.

## Key Files

_(none yet — project just started)_

## Learnings

### Test Infrastructure Setup (April 2026)

**Pattern: HttpClient mocking via DelegatingHandler**
- Created custom `CapturingHandler` and `StubGraphHopperHandler` classes that inherit from `HttpMessageHandler`
- These intercept HTTP calls without needing NSubstitute or other mocking frameworks
- More lightweight and predictable than IHttpClientFactory mocking
- Allows inspection of request URIs in tests (for query string validation)

**Pattern: WebApplicationFactory for API integration tests**
- Used `CustomWebApplicationFactory` to replace real GraphHopper client with stub
- Required adding `public partial class Program {}` to Program.cs for type reference
- Clean way to test entire API surface without external dependencies

**Test structure decisions:**
- Waymarked.Routing.Tests: 27 tests covering RouteRequest model, GraphHopperClient query string construction, distance conversions
- Waymarked.Api.Tests: 6 integration tests covering endpoint validation and happy paths
- Both use xUnit, FluentAssertions; avoided heavy mocking frameworks where possible

**No failures encountered** — test infrastructure worked first time after stopping Aspire (file lock on Waymarked.Api.exe)

### Round-Trip Accuracy Retry Tests (April 2026)

**Tests added for Brand's retry improvements:**
- `NoRetryWhenFirstAttemptWithinTolerance` — key regression guard: with MaxRetries=3 set, if the first route lands within tolerance no retry fires (previously this path had no test)
- `RespectsCustomTolerance` — verified DistanceTolerance=0.05 causes a retry on a 6%-off route but accepts 2%-off on the follow-up
- `AbRoute_DoesNotRetryRegardlessOfDistance` — A→B route with MaxRetries=3 still makes exactly 1 HTTP call (retry logic is bypassed)
- `Post_ApiRoutes_RoundTrip_AcceptsCustomDistanceTolerance` — API-level smoke test confirming `distanceTolerance` is a valid request parameter

**File lock pattern confirmed:** Stop Aspire processes before running `Waymarked.Api.Tests` (Api.dll is locked by the running Aspire project). Use `Stop-Process -Id <pid>` per PID shown in Aspire resource list.

**Total test count post-commit:** 60 (41 routing unit + 19 API integration)

### Auth Endpoint Tests (April 2026)

**Context:** Brand implemented ASP.NET Core Identity auth (`POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/logout`, `GET /api/auth/me`) using `WaymarkedDbContext` (IdentityDbContext), Npgsql via Aspire's `AddNpgsqlDbContext`, and cookie auth.

**Key infrastructure change — Aspire Npgsql in tests:**
- `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL`'s `AddNpgsqlDbContext` registers an `IConfigureOptions<DbContextOptions<WaymarkedDbContext>>` that validates the connection string at options-build time (not just at connection time)
- Simply removing `DbContextOptions<WaymarkedDbContext>` is NOT enough — the `IConfigureOptions<>` callback still fires and throws `InvalidOperationException: ConnectionString is missing`
- Fix: remove ALL registrations that reference `WaymarkedDbContext` (including `IConfigureOptions<DbContextOptions<WaymarkedDbContext>>`) then re-add with `UseInMemoryDatabase`
- `ConfigureTestServices` (not `ConfigureServices`) must be used — it runs AFTER Program.cs's service registration, guaranteeing our overrides win
- `Microsoft.EntityFrameworkCore.InMemory` added as test-only package

**New factories:**
- `AuthWebApplicationFactory` — stubs GraphHopper + InMemory DB, unique db name per instance for isolation
- `CustomWebApplicationFactory` — updated to also add InMemory DB (Brand's `EnsureCreatedAsync` startup call now requires it)

**Test file:** `AuthEndpointTests.cs` — 18 test cases (13 active, 5 skipped)

**Tests passing (13):**
- Register: happy path, duplicate email, missing email, invalid email format, password too short
- Login: happy path (200), auth cookie set, HttpOnly cookie, wrong password (401), unknown email (401)
- Logout: authenticated returns 200, unauthenticated returns 200 (idempotent)
- Me: authenticated returns 200 with email
- Full flow: register → login → /me

**Tests skipped — documented gaps for Brand (5):**
1. `Register_MissingPassword_Returns400` — null password causes unhandled exception (500). Brand must null-check password before `UserManager.CreateAsync`.
2. `Login_MissingEmail_Returns400` — no input validation; missing email returns 401 silently.
3. `Login_MissingPassword_Returns400` — same; missing password returns 401 silently.
4. `Me_Unauthenticated_Returns401` — `ConfigureApplicationCookie` is missing `OnRedirectToLogin` override; returns 302 (redirect to `/Account/Login`) instead of 401. API clients can't handle redirects. Brand must add: `options.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = 401; return Task.CompletedTask; };`
5. `Logout_ClearsSession_MeReturns401Afterwards` — depends on /me returning 401 (blocked by gap #4).

**Cookie inspection pattern:**
- `CreateClient()` (HandleCookies=true) — cookies persist between requests; Set-Cookie headers consumed by container, not visible in response
- `CreateRawClient()` (HandleCookies=false) — Set-Cookie headers visible; use when asserting cookie presence/flags (HttpOnly)

**Total test count post-commit:** 41 routing unit + 33 API integration (13 new auth) = 74 active; 5 auth tests skipped pending Brand fixes

### Auth E2E Journey Tests (April 2026)

**Context:** Added `AuthJourneyTests.cs` — 17 Playwright E2E tests covering the full auth UI against the real Aspire stack (PostgreSQL, API, web frontend).

**File:** `C:\Projects\waymarked\src\Waymarked.E2E.Tests\AuthJourneyTests.cs`

**Coverage:**
- Registration flow: modal opens on `#signInBtn` click; panel switches; submit disabled until pw requirements met; happy path registers and closes modal; duplicate email shows error
- Login flow: happy path; wrong password shows `#loginError.visible`; session persists across page reload
- Logout: `#signOutBtn` hides `#userInfo` and reveals `#signInBtn`
- Forgot password: panel navigation; always shows `#forgotSuccess` (anti-enumeration); `#forgotSubmit` disabled after submit
- Password reset: auto-opens modal from `?resetToken=&email=` URL params; submit disabled until valid password; invalid token shows `#resetError.visible`
- Modal UX: closes on Escape key; closes on `.auth-modal-overlay` click

**Patterns established:**
- All 17 tests are independent — each registers its own user via `RegisterViaUiAsync()` helper where needed (no direct API calls in E2E tests)
- Private helpers: `OpenAuthModalAsync`, `SwitchToRegisterAsync`, `RegisterViaUiAsync`, `SignOutAsync` — keeps test bodies focused and readable
- Uses `WaitForSelectorAsync("#element:not([hidden])")` and `WaitForFunctionAsync` for button enable/disable detection — avoids `Task.Delay`
- Playwright's `IsHiddenAsync()` / `IsVisibleAsync()` correctly handles the `hidden` HTML attribute used on auth modal elements
- `TestPassword = "ValidPass1!"` satisfies all ASP.NET Core Identity defaults (uppercase, lowercase, digit, non-alphanumeric, length ≥6)
- Unique emails via `$"e2e-{Guid.NewGuid():N}@waymarked.test"` (consistent with integration test pattern)

**File lock pattern (confirmed again):** Must stop `Waymarked.AppHost`, `Waymarked.Api`, and `Waymarked.Web` processes before building the E2E project. All have file locks on shared DLLs during normal Aspire operation.

**Total test count post-commit:** 41 routing unit + 33 API integration + 17 auth E2E = 91 active (+ 5 auth API skipped)

### Auth Hardening Integration Tests (April 2026)

**Context:** Brand implemented 4 pre-production auth security items:
1. Account lockout: 5 failed attempts → 15-minute lockout
2. Token lifespan: 24-hour expiration on reset/confirmation tokens
3. Rate limiting: `/api/auth/forgot-password` limited to 3 requests/15 minutes
4. Integration test expansion: 7 new tests for forgot-password and reset-password flows

**Coverage added to `AuthEndpointTests.cs`:**
- Forgot-password endpoint validation and rate limiting enforcement
- Reset-password endpoint with token validation
- All 7 tests passing; test infrastructure includes `FakeEmailSender` for email mocking
- Rate limiting disabled in test environment via configuration

**Total test count post-hardening:** 41 routing unit + 40 API integration (33 existing auth + 7 new hardening) + 17 E2E = 98 active (+ 5 auth API skipped)
