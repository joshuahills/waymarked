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
