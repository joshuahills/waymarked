# 🥾 Waymarked

[![CI](https://github.com/joshuahills/waymarked/actions/workflows/ci.yml/badge.svg)](https://github.com/joshuahills/waymarked/actions/workflows/ci.yml)

**UK Walk & Hike Route Planner** — plan walking and hiking routes across Great Britain. Enter a start point and a distance for a round-trip, or pick a destination for a point-to-point route.

Built with .NET 10, [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/), and [GraphHopper](https://www.graphhopper.com/).

---

## Features

- 🗺 **Interactive map** — Leaflet-based map centred on the UK
- 📍 **Place search** — search by address, postcode, or point of interest (via Nominatim/OSM)
- 🖱 **Map-click placement** — click anywhere on the map to set start or end point
- 🔄 **Round-trip routing** — set a start point and desired distance; get a loop route back
- ➡️ **Point-to-point routing** — set start and end for a direct route
- 🚶 **Walking & hiking profiles** — foot and hike profiles via GraphHopper
- 🇬🇧 **UK-only** — coordinates validated to Great Britain bounding box

## Screenshots

> _Coming soon_

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Aspire CLI](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/aspire-sdk-tooling) (`dotnet tool install -g aspire.cli`)
- Docker Desktop or [Podman](https://podman.io/) (for GraphHopper container)
- GraphHopper OSM data — see [`infra/graphhopper/README.md`](infra/graphhopper/README.md)

### Running

```bash
aspire run
```

This starts the full stack via .NET Aspire:
- **GraphHopper** container (routing engine, port 8989)
- **Waymarked API** (ASP.NET Core, `/api/routes`, `/api/bounds`)
- **Waymarked Web** (static frontend)

The [Aspire dashboard](http://localhost:15888) shows resource health, logs, and traces.

### First run

GraphHopper needs to import OSM data on first run (~5–40 min depending on extract size). Subsequent starts use the cached routing graph and are fast. See [`infra/graphhopper/README.md`](infra/graphhopper/README.md) for data setup.

---

## Project Structure

```
waymarked/
├── src/
│   ├── Waymarked.AppHost/          # .NET Aspire orchestrator
│   ├── Waymarked.Api/              # ASP.NET Core minimal API
│   ├── Waymarked.Routing/          # GraphHopper client + route models
│   ├── Waymarked.Web/              # Static frontend (Leaflet, HTML/CSS/JS)
│   ├── Waymarked.ServiceDefaults/  # Shared Aspire service config
│   ├── Waymarked.Api.Tests/        # API integration tests
│   ├── Waymarked.Routing.Tests/    # Unit tests
│   └── Waymarked.E2E.Tests/        # Playwright E2E tests (Aspire.Hosting.Testing)
├── infra/
│   └── graphhopper/                # GraphHopper config + OSM data
└── .github/
    └── workflows/
        └── ci.yml                  # GitHub Actions CI
```

---

## API

### `POST /api/routes`

Plan a route.

**Round-trip** (no `to` — returns a loop):
```json
{
  "from": [54.5994, -3.1367],
  "distance": 10.0,
  "distanceUnit": "kilometres",
  "profile": "hike"
}
```

**Point-to-point**:
```json
{
  "from": [54.5994, -3.1367],
  "to":   [54.4609, -3.0853],
  "profile": "foot"
}
```

**Response**:
```json
{
  "distanceKm": 10.06,
  "distanceMiles": 6.25,
  "durationFormatted": "2h 19m",
  "isRoundTrip": true,
  "points": { "type": "LineString", "coordinates": [[...], ...] },
  "instructions": [...]
}
```

### `GET /api/bounds`

Returns the GB bounding box used for coordinate validation.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Orchestration | .NET Aspire 9 |
| API | ASP.NET Core 10 (Minimal API) |
| Routing engine | GraphHopper 11.0 |
| Map data | OpenStreetMap |
| Geocoding | Nominatim (OSM) |
| Frontend | Leaflet, Vanilla JS |
| Testing | xUnit, Playwright |
| CI | GitHub Actions |

---

## Contributing

See [`src/README.md`](src/README.md) for developer notes, architecture details, and local setup.
