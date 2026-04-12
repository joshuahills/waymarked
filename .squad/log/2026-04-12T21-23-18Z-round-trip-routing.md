# Session Log: Round-Trip Routing Support

**Date:** 2026-04-12  
**Task:** Add round-trip routing support  
**Status:** ✅ Complete

## Summary

Brand successfully extended the routing system to support round-trip (circular) routes. Previously, RouteRequest required both start (A) and end (B) points. Now the end point is optional.

**Key changes:**
- `RouteRequest.To` is now nullable
- When omitted, GraphHopperClient uses GraphHopper's native `algorithm=round_trip` parameter
- Distance specified in metres via `round_trip.distance` (converted from km/mi in the API layer)
- Single-point A→B routing unchanged

## Deliverables

✅ Typed client and API layer updates  
✅ Both projects build successfully  
✅ Commit: 6a4ec8a

## Frontend Impact

Users can now plan routes by distance from a single start point without specifying an endpoint.
