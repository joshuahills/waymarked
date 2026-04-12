# Orchestration Log: data-osm-coverage

**Agent:** Data (GIS Engineering)  
**Timestamp:** 2026-04-12T16:00:00Z → 2026-04-12T17:15:00Z  
**Status:** COMPLETED (assessment), AWAITING REVIEW (decisions)

---

## Task

Assess UK OSM footpath coverage for walking route planner MVP. Identify gaps, recommend data strategy, document application-layer feature requirements for routing engine.

---

## What Was Delivered

### Artifacts

- `docs/osm-coverage-uk.md` — Comprehensive UK OSM footpath assessment with regional variation, coverage percentages, data source quality evaluation
- `decisions/inbox/data-osm-coverage.md` — Recommendations summary for team review

### Key Findings

**OSM + Valhalla sufficient for MVP & production** (with application-layer enhancements).

| Region | Coverage Quality | % Tagged |
|--------|------------------|----------|
| Lake District, Peak District, South Downs | Excellent | >80% PROW designated |
| Welsh uplands, long-distance paths | Good | 60-80% |
| Remote rural (Scottish Borders, mid-Wales) | Fair | 30-60% |
| Scotland (Right to Roam model) | 30-40% | Fewer paths tagged |

### Recommendations

#### 1. Development Data Strategy ✅ **DECISION READY**
- **Dev:** Scotland extract (`scotland-latest.osm.pbf`, ~100MB)
- **Prod:** Full GB extract (`great-britain-latest.osm.pbf`, ~1.5GB)
- **URLs:** Geofabrik weekly updates

#### 2. Valhalla Application-Layer Gaps ⚠️ **DESIGN DECISION NEEDED**

| Feature | Status | Requirement |
|---------|--------|-------------|
| Difficulty filtering (`sac_scale` T1–T6) | ❌ Unsupported | App-layer filtering |
| Seasonal closures | ❌ Unsupported | Curated closure database + UI warnings |
| Round-trip routing | ❌ Unsupported | Algorithm implementation or open-ended routes only |
| Permissive path warnings | ⚠️ Partial | UI flag for `foot=permissive` |
| CRoW access land routing | ⚠️ Partial | GeoJSON zones + overlay + warnings |

**MVP Question:** Support all features v1.0, or defer difficulty + seasonal to v1.1?

#### 3. UK PROW Coverage ✅ **FYI / EXPECTATIONS**
- Product will excel in popular regions (Lake District, Peak District, South Downs)
- Expect gaps in remote areas (Scottish Borders, mid-Wales, East Anglia)

#### 4. Data Contribution Opportunity 🔮
- Seasonal closure tagging (partner with Wildlife Trust, estates)
- CRoW zone mapping (GeoJSON/OSM)
- Surface/difficulty crowdsourcing (`sac_scale`, `trail_visibility`)

---

## Open Questions for Team

1. **MVP feature scope:** Difficulty filtering + seasonal closures v1.0, or defer to v1.1?
2. **Regional expectations:** How do we set user expectations for coverage gaps in remote areas?
3. **Data partnerships:** Worth exploring with Scottish Wildlife Trust, access land orgs?

---

## Alignment with Brand Spike

- **Convergence:** Both recommend Scotland extract for dev → aligns with brand-graphhopper-spike findings
- **Implication:** Routing engine choice (GraphHopper vs Valhalla) doesn't affect data strategy; Scotland remains optimal dev starting point

---

## Next Steps

1. **Team decision:** Review app-layer gaps table; decide MVP feature scope
2. **Data layer design:** Implement seasonal closures db, difficulty filtering logic
3. **Regional analysis:** Provide coverage breakdowns by authority boundary (e.g., Lake District, Pennines)
4. **User expectations docs:** Product documentation on coverage variation

---

## Handoff

**To:** Mikey (Lead), Josh (Product), Brand (Backend)

**Usefulness:** Critical for MVP scope; recommendations align with GraphHopper spike and inform Aspire service design (seasonal closures db, difficulty filtering microservice?).

---
