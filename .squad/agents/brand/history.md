# Brand — Project History

## Project Context

- **Project:** Waymarked — UK walking, hiking, and running route planner
- **Requested by:** Josh Hills
- **Goal:** Build a route planner app from scratch, primarily for UK use. Customisable options, map view of routes. Starting point: find suitable data source(s).
- **Stack:** TBD — no tech decisions made yet.
- **Status:** Team hired, work beginning.

## Key Files

- `infra/graphhopper/docker-compose.yml` — GraphHopper container setup
- `infra/graphhopper/config.yml` — Routing profiles (foot, hike), elevation config
- `infra/graphhopper/README.md` — Setup instructions, memory requirements, API examples
- `infra/graphhopper/test-route.sh` — Bash test script (Edinburgh route)
- `infra/graphhopper/test-route.ps1` — PowerShell test script (Edinburgh route)

## Core Context

### Backend Architecture Summary (2026-04-12 → 2026-04-14)

**Stack:** C# .NET 10 + Aspire 13, ASP.NET Core minimal APIs, PostgreSQL via EF Core Identity, GraphHopper Java routing engine in Docker container.

**Frontend:** Vanilla HTML/CSS/JS (no build step) hosted in ASP.NET Core static files, reverse-proxied via YARP to API (cookies transparent).

**Routing:** GraphHopper Docker container (local build from Maven Central JAR) exposes `/api/route` endpoint; Aspire orchestrates container lifecycle. Scotland OSM extract recommended for dev (~200MB, 5-10 min build), full GB for production.

**Auth:** ASP.NET Core Identity + PostgreSQL, cookie-based (HttpOnly, SameSite=Strict, 14-day sliding expiry). Register/Login/Logout/GetMe endpoints. Schema created via `EnsureCreatedAsync()` at startup (migrations future). Input validation: null guards + error response format (flat `{ errors: [...] }` array). Cookie `SecurePolicy.SameAsRequest` for HTTP dev + HTTPS prod compatibility.

**Observability:** OpenTelemetry instrumentation wired into all services via Aspire. EF Core + SQL Client tracing captures database queries as spans in dashboard.

**Round-Trip Routing:** GraphHopper native `round_trip` mode + client-side retry logic (up to 4 attempts, 15% distance tolerance default, seed diversity per retry).

**Distance Bounds Validation:** Client validates 500m–100km round-trip distance after unit conversion (km/miles → metres). A→B routes unaffected.

**Key Learning:** Token remapping in dark mode requires explicit pseudo-state overrides (e.g., button hover states need hardcoded `#ffffff` not `var(--clr-white)` which remaps).

---

## Learnings

### GraphHopper Dockerfile (Local Build) (2026-04-12)

**Why no official image:**
- GraphHopper does NOT publish to Docker Hub or GHCR; `graphhopper/graphhopper:latest` is a 404
- Official distribution is via JAR on Maven Central; self-hosting means building your own image

**Dockerfile approach:**
- Base: `eclipse-temurin:21-jre` (GraphHopper 11.x requires Java 17+; 21 is current LTS)
- JAR downloaded via `ADD` instruction from Maven Central at build time (no auth needed)
- ENTRYPOINT uses `sh -c` with `exec` to allow `$JAVA_OPTS` expansion from environment variable
- Pattern: `ENTRYPOINT ["sh", "-c", "exec java ${JAVA_OPTS} -jar ... \"$@\"", "sh"]` + `CMD ["--config", "..."]`
- No `USER` instruction to avoid Podman user namespace conflicts

**Aspire integration:**
- `AddDockerfile(name, contextPath)` replaces `AddContainer(name, image)` when no prebuilt image exists
- Aspire builds the image locally on first `aspire run`; subsequent runs use cached image
- `contextPath` is relative to the AppHost project directory

### GraphHopper Docker Setup (2026-04-12)

**Configuration Patterns:**
- GraphHopper uses a YAML config file for routing profiles, elevation providers, and bounds
- Profiles: `foot` (general walking on roads + footpaths) vs `hike` (prefers trails, avoids roads, uses SAC scale)
- Elevation: SRTM auto-downloads from NASA (90m resolution), but OS Terrain 50 is superior for UK (50m, but manual setup)
- UK bounding box: `49,-8,61,2` (lat_min,lon_min,lat_max,lon_max) speeds up graph processing

**Memory Requirements:**
- Scotland extract (~200MB): 2-3GB Java heap (`-Xmx2g`), ~4GB total RAM needed
- Great Britain full (~1.5GB): 8-12GB Java heap (`-Xmx8g`), ~16GB total RAM needed
- Graph build is CPU and disk I/O intensive; SSD strongly recommended
- Insufficient heap causes `OutOfMemoryError` during graph creation

**Graph Build Times:**
- Scotland: 5-10 minutes on modern hardware
- Full GB: 20-40 minutes
- First run: OSM parsing + graph building + elevation download
- Subsequent runs: Fast startup if graph cache exists in `./data/graph-cache/`

**Profile Behavior Differences:**
- **`foot`:** Routes on road network + footpaths; faster routes, suitable for urban/mixed terrain
- **`hike`:** Strongly prefers dedicated hiking paths/trails; avoids roads; uses OSM `sac_scale` for difficulty rating
- Both profiles support elevation-aware routing with SRTM
- Round-trip routing supported natively (unlike Valhalla)

**Docker Gotchas:**
- Healthcheck needs `start_period: 300s` for initial graph build (startup can take 20-40 min for GB extract)
- Elevation download can fail/stall if NASA servers are slow; tiles cached in `elevation-cache/` after first success
- Config changes require deleting graph cache and rebuilding
- Port 8989 is default; healthcheck endpoint at `/health`

**API Features:**
- Points encoded by default (encoded polyline); use `points_encoded=false` for plain JSON coordinates
- Elevation in responses: `elevation=true` parameter
- Turn-by-turn instructions: `instructions=true&locale=en-GB`
- Alternative routes: `algorithm=alternative_route&alternative_route.max_paths=3`
- Round-trip: `round_trip.distance=10000&round_trip.seed=0`

### .NET Aspire Project Scaffolding (2026-04-12)

**Project Structure:**
- Used `dotnet new aspire-starter` template to scaffold initial solution
- .NET SDK 10.0.201 with Aspire workload installed (13.1.0-preview.1)
- Removed auto-generated `Waymarked.Web` Blazor frontend (not needed)
- Renamed `Waymarked.ApiService` → `Waymarked.Api` for consistency
- Created `Waymarked.Routing` class library for GraphHopper client abstraction

**Solution Layout:**
```
src/
  Waymarked.AppHost/          - .NET Aspire orchestrator (DistributedApplication)
  Waymarked.Api/              - ASP.NET Core Web API (minimal APIs)
  Waymarked.ServiceDefaults/  - Shared Aspire defaults (health checks, telemetry)
  Waymarked.Routing/          - GraphHopper HTTP client + models
```

**GraphHopper Container Integration:**
- AppHost configures GraphHopper as a container resource using `AddContainer()`
- Bind mounts: `config.yml` (read-only) and `data/` (read-write for OSM + graph cache)
- HTTP endpoint exposed on port 8989
- Java heap configured via `JAVA_OPTS` environment variable (`-Xmx2g -Xms2g`)
- API project references GraphHopper endpoint via Aspire's service discovery (`WithReference()`)

**GraphHopper Client Pattern:**
- Strongly-typed `HttpClient` wrapper: `GraphHopperClient`
- Request/response models: `RouteRequest`, `RouteResponse`, `RoutePath`, `RoutePoints`, `RouteInstruction`
- JSON deserialization using System.Text.Json with `[JsonPropertyName]` attributes
- DI registration via extension method: `AddGraphHopperClient(baseAddress)`
- BaseAddress configured from Aspire connection strings (`ConnectionStrings:graphhopper`)

**API Endpoint:**
- `POST /api/routes` — minimal API endpoint for route planning
- Accepts: `{ "from": [lat, lon], "to": [lat, lon], "profile": "hike" }`
- Returns: GraphHopper route response with distance, time, elevation, points (GeoJSON), instructions
- Error handling: Returns HTTP 502 Bad Gateway if routing engine is unreachable

**Package Dependencies:**
- `Waymarked.Routing` NuGet packages:
  - `Microsoft.Extensions.Http` 10.0.5 (HttpClient factory)
  - `Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.5
  - `Microsoft.Extensions.Logging.Abstractions` 10.0.5
- `Waymarked.Api` references `Waymarked.Routing` and `Waymarked.ServiceDefaults`
- `Waymarked.AppHost` SDK: `Aspire.AppHost.Sdk/13.1.0-preview.1`

**Build Warnings (Non-Blocking):**
- `ASPDEPR002`: `WithOpenApi()` is deprecated in .NET 10 (consider removing or replacing)
- `ASPIRE004`: ServiceDefaults project referenced by AppHost but not executable (expected, can set `IsAspireProjectResource="false"` to suppress)

**Aspire Connection String Pattern:**
- GraphHopper endpoint reference is passed to API via Aspire's service discovery
- API reads connection string: `builder.Configuration.GetConnectionString("graphhopper")`
- Aspire automatically injects the HTTP endpoint URL at runtime

**Data Directory Setup:**
- Created `infra/graphhopper/data/.gitkeep` to track empty directory
- OSM extracts and graph cache will be stored in `data/` (gitignored)
- Bind mount is read-write so GraphHopper can build/cache graph on first run

### Round-Trip Routing Support (2026-04-12)

**GraphHopper Round-Trip Mode:**
- GraphHopper supports native round-trip routing via `algorithm=round_trip` query parameter
- Parameters: `round_trip.distance` (metres), `round_trip.seed` (random integer for route variety)
- Only requires a single `point=` (start location); no destination needed
- Distance is specified in metres; seed controls route generation randomness

**API Design — Distance Unit Conversion:**
- Client sends distance in km or miles via `DistanceUnit` field ("kilometres" or "miles")
- API layer converts to metres before passing to `GraphHopperClient`:
  - 1 km = 1000 m
  - 1 mile = 1609.344 m
- Internal `RouteRequest.Distance` is always in metres (double?)
- Separation of concerns: client-facing units vs internal representation

**Model Changes:**
- Added `DistanceUnit` enum: `Kilometres | Miles` (in `RouteRequest.cs`)
- Made `RouteRequest.To` optional (`double[]?`) to support single-point round trips
- Added `RouteRequest.Distance` (nullable double, in metres)
- Added `RouteRequest.DistanceUnit` property (kept for reference, conversion happens in API layer)

**GraphHopperClient Branching:**
- Client now branches on `request.To == null`:
  - **A→B mode (To is NOT null):** Existing behavior — append `&point={To}`, no algorithm param
  - **Round-trip mode (To IS null):** Use `algorithm=round_trip`, `round_trip.distance`, `round_trip.seed`
- Uses `Random.Shared.Next()` for seed generation (different route each request)
- Logs round-trip mode activation with distance for debugging

**API Request Model:**
- Created `ApiRouteRequest` record in `Program.cs`:
  - `From: double[]` (required)
  - `To: double[]?` (optional)
  - `Distance: double?` (client units, km or miles)
  - `DistanceUnit: string?` (default "kilometres")
  - `Profile: string?` (default "hike")
- Validation:
  - `From` must be non-null and contain at least 2 elements (lat, lon)
  - If `To` is null, `Distance` must be > 0 (round-trip requires distance)
- Returns `Results.ValidationProblem` with clear messages on validation failure

**Backward Compatibility:**
- A→B routing unchanged: existing clients sending `From` + `To` work exactly as before
- Round-trip is opt-in: only triggered when `To` is omitted

### WaymarkedRouteResponse and Distance Validation (2026-04-12)

**WaymarkedRouteResponse Type:**
- Added new record type in `Waymarked.Routing` namespace
- Provides client-friendly derived fields from GraphHopper's raw response
- Properties:
  - `Distance` (double): raw metres from GraphHopper
  - `DistanceKm` (double): metres ÷ 1000, rounded to 2 decimal places
  - `DistanceMiles` (double): metres ÷ 1609.344, rounded to 2 decimal places
  - `DurationMs` (long): milliseconds from GraphHopper (already in ms, no conversion needed)
  - `DurationFormatted` (string): human-readable format (e.g., "1h 23m" or "45m")
  - `Points` (RoutePoints?): GeoJSON LineString from first path
  - `Instructions` (RouteInstruction[]?): turn-by-turn instructions
  - `IsRoundTrip` (bool): whether this was a round-trip request
- Factory method: `FromRouteResponse(RouteResponse response, bool isRoundTrip)`
- Duration formatting: shows hours only if >= 60 minutes, always shows minutes

**Distance Bounds Validation:**
- Validates round-trip distance AFTER unit conversion (in metres)
- Minimum: 500 metres (0.5 km)
- Maximum: 100,000 metres (100 km)
- Only validates when `distanceInMetres.HasValue` (A→B routes unaffected)
- Returns `Results.ValidationProblem` with descriptive error messages
- Validation happens post-conversion, so bounds are consistent regardless of input unit

**Coordinate Validation Placeholder:**
- TODO comment added in `Program.cs` before route request construction
- Reserved space for Data (GIS team) to add UK bounding box validation
- Keeps validation concerns separated: distance (Backend) vs coordinates (Data)

### Round-Trip Distance Accuracy Investigation (2026-04-13)

**Root Cause:**
GraphHopper's round-trip algorithm is probabilistic — it cannot guarantee returned distance matches requested distance. Three gaps in the old implementation: no `round_trip.max_retries` set (defaulted to 3), no client-side deviation check, and a single fixed seed per request.

**What Was Fixed:**
- Added `round_trip.max_retries=10` to every round-trip GraphHopper query (up from default 3)
- Added client-side retry loop: if returned distance deviates >15% from requested, retry with a new random seed (up to 3 retries = 4 total attempts)
- Added `MaxRetries` and `DistanceTolerance` to `RouteRequest`; API sets MaxRetries=3 for round-trip, 0 for A→B
- Exposed `distanceTolerance` as an optional API request parameter (default 0.15)
- Extracted `SendAsync` private method to avoid duplicating HTTP logic in the retry loop
- 4 new unit tests: max_retries in query, full retry exhaustion, early-stop on tolerance met, seed diversity

**Distance Unit Audit:**
No bugs found. Conversion (km/miles → metres) and validation (500m–100km) were already correct.

**What Requires Data's Input:**
- OSM path network density — sparse networks prevent long loops; client retries can't fix data gaps
- GraphHopper routing profile weight tuning in config.yml (hike profile)
- Elevation-corrected distance is a product decision (network km ≠ effort km in hilly terrain)

**Tests:** 38 Waymarked.Routing.Tests passing (was 34). Committed: 8dd8051

### ASP.NET Core Identity + PostgreSQL Auth (2026-04-14)

**Packages used:**
- `Aspire.Hosting.PostgreSQL` (13.2.2) — AppHost resource definition
- `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` (13.2.2) — Aspire client integration for EF Core + Npgsql
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore` (10.0.5) — Identity with EF stores
- All added via Central Package Management in `Directory.Packages.props`

**Aspire pattern for Postgres:**
- AppHost: `builder.AddPostgres("db").AddDatabase("waymarked")` — creates a named database resource
- API receives: `.WithReference(db).WaitFor(db)` — injects connection string, waits for DB readiness
- API reads: `builder.AddNpgsqlDbContext<WaymarkedDbContext>("waymarked")` — Aspire resolves the connection string by name

**Identity setup order matters:**
- `AddNpgsqlDbContext<T>` must be called before `AddIdentity<T, TRole>` to register the DbContext first
- `AddIdentity` (not `AddIdentityCore`) automatically configures cookie authentication as the default scheme
- `ConfigureApplicationCookie` must be called AFTER `AddIdentity` to override defaults
- `AddAuthorization()` added explicitly for `.RequireAuthorization()` on endpoints

**Cookie auth config:**
- `HttpOnly = true`, `SameSite = Strict`, `SlidingExpiration = true`, `ExpireTimeSpan = 14 days`
- Cookie auth only — no JWTs; YARP passes cookies transparently to downstream API

**EnsureCreated at startup:**
- Used `IServiceScope` to resolve `WaymarkedDbContext` and call `EnsureCreatedAsync()` after `Build()`
- This creates schema on first run; migrations are a future concern
- Scope is properly disposed via `using` block

**Auth endpoints (`/api/auth`):**
- `POST /register` — creates user via `UserManager<ApplicationUser>`, returns 400 validation problem on failure
- `POST /login` — uses `SignInManager.PasswordSignInAsync`, no lockout for now (lockoutOnFailure: false)
- `POST /logout` — `SignInManager.SignOutAsync`, always returns 200
- `GET /me` — checks `User.Identity.IsAuthenticated`, returns email; guarded with `.RequireAuthorization()`

**File structure:**
- `src/Waymarked.Api/Data/ApplicationUser.cs` — minimal `IdentityUser` subclass
- `src/Waymarked.Api/Data/WaymarkedDbContext.cs` — primary key constructor, inherits `IdentityDbContext<ApplicationUser>`
- `src/Waymarked.Api/AuthEndpoints.cs` — static extension class with `MapAuthEndpoints()`, request records co-located

**Build result:** `Build succeeded. 0 Error(s)`

### Auth Endpoint Bug Fixes — Register 400 (2026-04-14)

**Problem:** Josh was getting 400 on `POST /api/auth/register`. Three bugs found and fixed:

1. **Null password → 500 (unhandled exception):** `UserManager.CreateAsync(user, null)` throws `ArgumentNullException`. Added explicit null/empty checks for both Email and Password before calling Identity — returns `400 { errors: ["..."] }` instead of crashing.

2. **`/me` and protected routes returning 302 instead of 401:** `AddIdentity` sets up cookie auth with default redirect behaviour. `ConfigureApplicationCookie` was missing `OnRedirectToLogin` and `OnRedirectToAccessDenied` event overrides. Added both — API routes now return 401/403 instead of redirecting.

3. **Login missing input validation:** `PasswordSignInAsync(null, ...)` fails silently and returns 401. Added null/empty guard on Email + Password — missing fields now return `400 { errors: ["Email and password are required."] }`.

**Error format change:** Register errors were grouped by Identity error code (`ValidationProblem` dictionary). Changed to flat `{ errors: ["..."] }` array via `Results.BadRequest(new { errors = ... })`.

**Test result:** 5 previously-skipped tests are now passing. Total: 19/19 passed, 0 skipped.

**Files changed:**
- `src/Waymarked.Api/AuthEndpoints.cs` — null guards on register + login, simplified error format
- `src/Waymarked.Api/Program.cs` — `OnRedirectToLogin` + `OnRedirectToAccessDenied` added to cookie options
- `src/Waymarked.Api.Tests/AuthEndpointTests.cs` — removed `[Fact(Skip=...)]` from 5 now-fixed tests

### Auth Cookie Bug Fixes — Register Sign-In + SecurePolicy (2026-04-14)

**Problem:** Users weren't staying signed in after registration. Dev tools showed no cookies being saved. Two bugs identified:

1. **Register endpoint didn't issue an auth cookie:** `POST /api/auth/register` called `userManager.CreateAsync` but never called `signInManager.SignInAsync`. No cookie was issued, so the frontend's optimistic auth state was wiped on page refresh when `/api/auth/me` returned 401.

2. **Cookie `SecurePolicy` blocked cookies on HTTP in dev:** Aspire dev runs on HTTP (not HTTPS). ASP.NET Core's default `SecurePolicy` is `Always` — it sets the `Secure` cookie attribute, causing browsers to refuse to store/send the cookie over plain HTTP connections. Added `CookieSecurePolicy.SameAsRequest` so the cookie is usable on HTTP in dev and HTTPS in production.

**Fixes applied:**

- `AuthEndpoints.cs`: Added `SignInManager<ApplicationUser> signInManager` parameter to the `/register` lambda. After successful `CreateAsync`, calls `await signInManager.SignInAsync(user, isPersistent: true)`.

- `Program.cs`: Added `options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;` inside `ConfigureApplicationCookie`. `SameSite=Strict` retained — all browser requests go through YARP on the same origin, so Strict is safe and correct.

**Build:** All errors were file-lock conflicts from the live Aspire process (PID 58648). Zero compilation errors in the changed code.

### OpenTelemetry EF Core + SQL Client Instrumentation (2026-04-14)

**What was added:**
- `OpenTelemetry.Instrumentation.EntityFrameworkCore` (1.15.0-beta.1) — EF Core queries appear as spans
- `OpenTelemetry.Instrumentation.SqlClient` (1.15.1) — raw ADO.NET SQL calls captured as spans
- Both packages pinned in `src/Directory.Packages.props` and referenced in `Waymarked.ServiceDefaults.csproj`
- `.AddEntityFrameworkCoreInstrumentation()` and `.AddSqlClientInstrumentation()` added to the tracing builder in `Extensions.cs`

**Npgsql note:**
- Npgsql has first-class OTel support. The Aspire `AddNpgsqlDbContext` integration wires up Npgsql tracing automatically via the data source — no extra `AddNpgsql()` call needed in ServiceDefaults.
- `AddSqlClientInstrumentation()` is added for completeness (ADO.NET layer); Npgsql traces come through the Aspire integration.

**Package version alignment:**
- Versions chosen to match the existing OTel 1.15.x family already in use.

**Build result:** `Build succeeded. 0 Error(s)` (21 pre-existing warnings, 0 errors)


### Auth Hardening: Lockout, Token Lifespan, Rate Limiting, Tests (2026-04-14)

**What was implemented:**
1. Enabled account lockout on failed login attempts (5 attempts → 15 min lockout)
2. Configured password reset token lifespan (24 hours via DataProtectionTokenProviderOptions)
3. Added rate limiting to forgot-password endpoint (3 requests per 15 minutes, fixed window)
4. Added 7 integration tests for forgot-password and reset-password endpoints

**Lockout configuration (Program.cs):**
- MaxFailedAccessAttempts = 5
- DefaultLockoutTimeSpan = 15 minutes  
- AllowedForNewUsers = true
- lockoutOnFailure: true in PasswordSignInAsync (AuthEndpoints.cs line 40)

**Token lifespan (Program.cs):**
- DataProtectionTokenProviderOptions.TokenLifespan = 24 hours
- Applies to password reset tokens generated via GeneratePasswordResetTokenAsync

**Rate limiting implementation:**
- Uses ASP.NET Core AddRateLimiter with FixedWindowLimiter policy
- Policy name: 'forgot-password', applied via .RequireRateLimiting() on endpoint
- Disabled in Test environment to avoid cross-test interference
- Returns 429 (Too Many Requests) when limit exceeded

**Test implementation:**
- Added FakeEmailSender in test factory to replace SmtpEmailSender (avoids SMTP connection in tests)
- 4 forgot-password tests: known email, unknown email, empty email, null body (all return 200 for anti-enumeration)
- 3 reset-password tests: invalid token, missing fields, unknown email (all return 400)
- All 7 new tests passing; 3 pre-existing test failures unrelated to this work

**Files modified:**
- src/Waymarked.Api/Program.cs — lockout config, token lifespan, rate limiting services + middleware
- src/Waymarked.Api/AuthEndpoints.cs — lockoutOnFailure: true, rate limiting on forgot-password endpoint  
- src/Waymarked.Api.Tests/AuthEndpointTests.cs — 7 new integration tests
- src/Waymarked.Api.Tests/AuthWebApplicationFactory.cs — FakeEmailSender, SMTP settings, lockout disabled in tests

**Build result:** Build succeeded. All tests: 45 total, 42 passed, 3 failed (pre-existing).

### Email Stub Cleanup + C# Comment Polish (2026-04-14)

**What was done:**
- `SendConfirmationLinkAsync` in `SmtpEmailSender` replaced with `Task.CompletedTask` + comment explaining it's an interface stub (email confirmation is not used; `/register` auto-signs-in).
- `SendPasswordResetCodeAsync` replaced with `Task.CompletedTask` + comment (code-based reset not implemented).
- `SendPasswordResetLinkAsync` and `SendWelcomeEmailAsync` left untouched (both actively used).
- `FakeEmailSender` in tests cleaned up — all four methods collapsed to expression-body `Task.CompletedTask` (no "No-op for tests." comments needed).
- `Program.cs` stripped of restatement comments (`// Add service defaults`, `// Database and Identity`, `// Email`, `// Configure the HTTP request pipeline`, etc.). Kept comments explaining WHY (rate limiting test bypass, CI elevation flag, `// Ensure the database schema exists (migrations come later)`).
- `AuthEndpointTests.cs` had one leftover `// Missing token and newPassword.` comment removed.

**Rule reinforced:** Keep comments that say WHY (security, workarounds, non-obvious choices). Remove comments that restate the code.

**Build result:** Compilation clean. Tests: 45 total, 42 passed, 3 failed (pre-existing, unrelated).



**What was implemented:**
- Added `IWaymarkedEmailSender` interface in `Waymarked.Api/Email/IWaymarkedEmailSender.cs` with a single `SendWelcomeEmailAsync(ApplicationUser user, string email)` method. Kept separate from `IEmailSender<ApplicationUser>` (which is a fixed ASP.NET Identity contract).
- `SmtpEmailSender` now implements both `IEmailSender<ApplicationUser>` and `IWaymarkedEmailSender`.
- `SendWelcomeEmailAsync` sends a simple "Welcome to Waymarked! Your account is all set." HTML email.
- `/api/auth/register` endpoint injects `IWaymarkedEmailSender` and calls `SendWelcomeEmailAsync` **fire-and-forget** after sign-in: `_ = emailSender.SendWelcomeEmailAsync(...).ContinueWith(t => logger.LogError(...), TaskContinuationOptions.OnlyOnFaulted)`.
- Email failure never blocks registration or sign-in — `Results.Ok()` is returned regardless.

**DI registration:**
- `Program.cs`: Added `builder.Services.AddTransient<IWaymarkedEmailSender, SmtpEmailSender>()` alongside the existing `IEmailSender<ApplicationUser>` registration.

**Test factory updates (`AuthWebApplicationFactory.cs`):**
- `FakeEmailSender` now implements both `IEmailSender<ApplicationUser>` and `IWaymarkedEmailSender`.
- Registered as a singleton via concrete type first, then resolved for both interfaces — avoids two separate singleton instances.

**ILogger note:**
- `AuthEndpoints` is a `static class`. Cannot be used as a type argument (e.g., `ILogger<AuthEndpoints>`). Injected `ILoggerFactory` instead and called `loggerFactory.CreateLogger("AuthEndpoints")`.

**Build result:** Compiled successfully. Tests: 45 total, 42 passed, 3 failed (pre-existing, unrelated to email).

### Code Quality Review Findings + Cleanup (2026-04-14)

**Status:** COMPLETED

**Findings from Mikey's full codebase review:**
- Email infrastructure is clean. `SendConfirmationLinkAsync` and `SendPasswordResetCodeAsync` are interface-required but never called by app code. Correct and harmless.
- Export endpoints duplicate the validate→build→execute pipeline (DRY violation). Not urgent but will bite when adding new formats.

**Comment cleanup applied:**
- Removed `// Add services to the container.` (Program.cs, boilerplate)
- Removed `// Learn more about configuring OpenAPI...` (Program.cs, boilerplate)
- Kept all security comments and WHY-based explanations

**Status:** Brand completed email stub honest-ops and verbose comment removal across Program.cs, AuthWebApplicationFactory.cs, AuthEndpointTests.cs

