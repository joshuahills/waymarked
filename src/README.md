# Waymarked Solution

[![CI](https://github.com/joshuahills/waymarked/actions/workflows/ci.yml/badge.svg)](https://github.com/joshuahills/waymarked/actions/workflows/ci.yml)

Aspire-based solution for the Waymarked UK walking/hiking/running route planner.

## Projects

- **Waymarked.AppHost** — Aspire orchestrator that manages GraphHopper container and API service
- **Waymarked.Api** — ASP.NET Core Web API with routing endpoints
- **Waymarked.Routing** — Class library containing GraphHopper HTTP client and routing models
- **Waymarked.ServiceDefaults** — Shared Aspire service defaults (health checks, telemetry, service discovery)
- **Waymarked.Web** — Static frontend (Leaflet, Vanilla JS, HTML/CSS)
- **Waymarked.Api.Tests** — API integration tests (xUnit + WebApplicationFactory)
- **Waymarked.Routing.Tests** — Routing unit tests (RouteRequest, RouteExporter, GraphHopperClient, DistanceConversion)
- **Waymarked.E2E.Tests** — Playwright end-to-end tests (Aspire.Hosting.Testing)

## Running the Application

### Prerequisites

- .NET 10.0 SDK
- Aspire CLI (version 13.2.2): `dotnet tool install -g aspire.cli`
- Docker Desktop or compatible container runtime
- GraphHopper OSM data in `../infra/graphhopper/data/` (see Data team for setup)

### Start the Application

```bash
aspire run
```

Run from the repo root. This will:
1. Start the GraphHopper container with configured routing profiles
2. Start the Waymarked API service
3. Open the Aspire dashboard (usually at `http://localhost:15888`)

### API Endpoints

- **Health:** `GET http://localhost:<port>/health`
- **Plan Route:** `POST http://localhost:<port>/api/routes`

Example request:
```json
{
  "from": [55.9533, -3.1883],
  "to": [55.9445, -3.1892],
  "profile": "hike",
  "elevation": true,
  "instructions": true
}
```

## Architecture

```
┌─────────────────────┐
│  Waymarked.AppHost  │  Aspire orchestrator
└──────────┬──────────┘
           │
           ├─► GraphHopper Container (port 8989)
           │   └─ Java routing engine
           │
           ├─► Waymarked.Api (ASP.NET Core)
           │   └─ uses Waymarked.Routing library
           │      └─ GraphHopperClient (HttpClient wrapper)
           │
           └─► Waymarked.Web (static frontend)
               └─ Leaflet, Vanilla JS, HTML/CSS
```

## Development Notes

- GraphHopper data directory: `infra/graphhopper/data/` (bind-mounted into container)
- GraphHopper config: `infra/graphhopper/config.yml` (routing profiles, elevation settings)
- First run: GraphHopper builds routing graph from OSM data (~5-40 min depending on extract size)
- Subsequent runs: Fast startup using cached graph

## Testing

Run all tests from the `src/` directory:

```bash
dotnet test
```

Test projects:
- **Waymarked.Api.Tests** — API integration tests with xUnit and WebApplicationFactory
- **Waymarked.Routing.Tests** — Routing unit tests covering RouteRequest, RouteExporter, GraphHopperClient, and DistanceConversion
- **Waymarked.E2E.Tests** — End-to-end tests using Playwright and Aspire.Hosting.Testing

## See Also

- `infra/graphhopper/README.md` — GraphHopper setup and API reference
