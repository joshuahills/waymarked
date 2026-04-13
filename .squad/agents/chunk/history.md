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
