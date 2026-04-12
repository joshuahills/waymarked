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
