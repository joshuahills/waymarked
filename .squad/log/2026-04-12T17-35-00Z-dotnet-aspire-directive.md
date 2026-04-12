# Session Log: .NET/Aspire Tech Stack Directive

**Timestamp:** 2026-04-12T17:35:00Z  
**Entry:** Scribe (documentation)

---

## Summary

Josh confirmed C# / .NET backend stack with Aspire as the orchestration target. Brand Docker spike (GraphHopper) completed successfully and provides reference implementation. Aspire spike spawned to scaffold .NET Aspire solution infrastructure.

---

## Decisions Captured

### 🔒 LOCKED: Backend Stack = C# / .NET / Aspire

- **Decision Owner:** Josh Hills (Product)
- **Decision Type:** Binding technical directive
- **Scope:** Backend API, local dev orchestration

**What:**
- Backend API: ASP.NET Core (replaces Node.js/Express or Python/FastAPI)
- Local orchestration: .NET Aspire (replaces Docker Compose as primary target)
- Container services (GraphHopper, databases, etc.) run as resources within Aspire app host
- All new backend code in C#

**Why:**
- Explicit user preference and product direction

**Implications:**
- Brand's Docker Compose spike remains useful as reference/fallback but Aspire is the end goal
- Coordinate with Brand to ensure Aspire integration plan includes GraphHopper container resource
- All future backend contributions should target C#/.NET

---

## Spikes in Flight

### ✅ brand-graphhopper-spike — COMPLETED

**Owner:** Brand (Backend Dev)  
**Status:** Completed  
**Artifacts:** `infra/graphhopper/` with Docker Compose, config, test scripts

**Key Decisions:**
- Scotland OSM extract recommended for dev (faster iteration, lower resource requirements)
- GraphHopper `foot` and `hike` profiles enabled
- SRTM elevation (production: defer to OS Terrain 50)

**Impact:** Validates GraphHopper as routing engine; Docker Compose setup usable as Aspire migration reference.

### ⏳ brand-aspire-spike — SPAWNED (in progress)

**Owner:** Brand (Backend Dev)  
**Objective:** Scaffold .NET Aspire application host with services:
- ASP.NET Core API service
- GraphHopper container resource (reference Docker Compose setup)
- Local database (PostgreSQL or SQLite for spike)
- Service discovery / health checks

**Expected Outcome:** Working Aspire app that orchestrates GraphHopper + API locally.

---

## Coordination Notes

- **To Brand:** Use Docker Compose spike as GraphHopper container reference; focus Aspire spike on service integration
- **To Data (GIS):** Continue UK OSM coverage analysis; recommendations align with GraphHopper decisions (Scotland extract for dev)
- **Decision inbox merged:** All spike decisions consolidated to `decisions.md` with Aspire directive locked

---
