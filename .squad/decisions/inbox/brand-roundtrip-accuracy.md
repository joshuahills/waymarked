# Round-Trip Distance Accuracy — Findings & Improvements

**Author:** Brand (Backend Dev)  
**Date:** 2026-04-13  
**Status:** Implemented (client-side) | Pending (server-side — needs Data)

---

## What Was Wrong

GraphHopper's round-trip algorithm is probabilistic. It accepts a `round_trip.distance` (in metres) and internally tries different route shapes using a seed value. The algorithm cannot guarantee the returned route exactly matches the requested distance — it depends on the local road/path network. The old implementation made a single attempt with a random seed and returned whatever GraphHopper came back with, regardless of how far off the distance was.

Three issues were identified:

1. **No internal retry budget** — `round_trip.max_retries` was not being set, so GraphHopper defaulted to 3 internal attempts. This is low.
2. **No client-side tolerance check** — if GraphHopper returned a 5km route when 10km was requested, we served it anyway.
3. **No seed diversity on failure** — the seed was generated once and never changed. Retrying with the same seed hits the same network paths.

---

## What Was Fixed (Client Side — Committed: 8dd8051)

### 1. GraphHopper `round_trip.max_retries=10`
Added to every round-trip query. Tells GraphHopper to try 10 candidate routes internally per call (up from the default of 3). Costs a small amount of extra CPU on the GH side but gives it more chances to find a better match before returning.

### 2. Client-Side Retry Loop
`GraphHopperClient` now retries round-trip requests with a **fresh random seed** if the returned distance deviates from the requested distance by more than `DistanceTolerance` (default 15%). The API sets `MaxRetries = 3` for all round-trip requests, so up to 4 total attempts can be made.

- If any attempt lands within tolerance, it returns immediately (no wasted calls).
- If all retries are exhausted, the last result is returned (graceful degradation).
- Each retry generates a new seed, so it explores genuinely different route shapes.

### 3. `DistanceTolerance` API Parameter
Clients can now pass `distanceTolerance` (fractional, e.g. `0.10` = ±10%) in the route request. Defaults to `0.15` (±15%). Tighter values will trigger more retries; looser values accept a wider band and return faster.

### 4. New Properties on `RouteRequest`
- `MaxRetries` (int, default 0) — number of client-side retries. API sets 3 for round-trip, 0 for A→B.
- `DistanceTolerance` (double, default 0.15) — fractional deviation threshold.

### 5. Unit Tests
4 new tests added to `GraphHopperClientTests`:
- `GetRouteAsync_RoundTrip_IncludesMaxRetriesParam` — verifies `round_trip.max_retries=10` is in the query
- `GetRouteAsync_RoundTrip_RetriesUpToMaxWhenDeviationExceedsTolerance` — verifies 4 total attempts when tolerance never met
- `GetRouteAsync_RoundTrip_StopsRetryingWhenDistanceWithinTolerance` — verifies early exit when second attempt succeeds
- `GetRouteAsync_RoundTrip_SeedDiffersBetweenRetries` — verifies each retry uses a different seed

---

## What Requires Data's Input

These are server-side GraphHopper concerns, outside Brand's scope:

### A. OSM Path Network Density
The root cause of large distance deviations is sparse path networks. If a user requests a 15km round trip in an area with few connected footpaths, GraphHopper cannot construct a 15km loop — it falls back to shorter available loops. No amount of client retries or parameter tuning fixes a data gap. **Data should validate OSM footpath coverage in key UK regions** and flag areas where round-trip routing will be unreliable.

### B. GraphHopper Routing Profile Tuning
The `hike` profile may need custom costing weights to prefer longer connecting paths over repeated short ones. This is configured in `infra/graphhopper/config.yml` (Data/Infra concern). More aggressive exploration weights could help the algorithm find longer loops.

### C. Elevation-Aware Distance
GraphHopper's `round_trip.distance` is **network distance**, not Euclidean or elevation-corrected distance. In hilly terrain, a 10km network route has significantly more real-world effort. Whether to correct for this (e.g. multiply requested distance by a grade factor) is a product decision. If pursued, it would live in `Program.cs` `BuildRouteRequest`.

### D. Alternative Route Candidates
GraphHopper can return multiple route alternatives (`alternative_route.max_paths`). A future improvement could request 3 round-trip alternatives per call and select the one closest to the requested distance. This is more complex (response shape changes) and would be a follow-on task.

---

## Distance Unit Audit (No Issues Found)

- Client sends distance in km or miles via `distanceUnit` field
- `Program.cs` converts to metres before building `RouteRequest`
- `GraphHopperClient` passes metres directly as `round_trip.distance` (integer, rounded via `Convert.ToInt32`)
- GraphHopper API spec: metres ✅

No bugs found here. The conversion is correct and validated between 500m and 100,000m before the request is made.

---

## Summary

| Improvement | Scope | Status |
|---|---|---|
| `round_trip.max_retries=10` | Client | ✅ Implemented |
| Client-side retry on deviation | Client | ✅ Implemented |
| Seed diversity across retries | Client | ✅ Implemented |
| `distanceTolerance` API param | API | ✅ Implemented |
| OSM network coverage validation | Data | ⏳ Needs Data |
| Routing profile weight tuning | Infra/Data | ⏳ Needs Data |
| Elevation-corrected distance | Product decision | ⏳ TBD |
| Multi-alternative selection | Client (future) | ⏳ Not started |
