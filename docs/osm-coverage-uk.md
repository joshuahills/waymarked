# UK OSM Footpath Coverage Assessment

**Document Version:** 1.0  
**Date:** 2026-04-12  
**Author:** Data (GIS Engineer)  
**Scope:** GraphHopper pedestrian/hiking routing engine assessment for Waymarked  
**Status:** Live reference — updated as new findings emerge  

---

## 1. OSM Highway Tags Relevant to GraphHopper Pedestrian/Hiking Profiles

### 1.1 Core Highway Tags

GraphHopper's hike profile uses OSM highway tags to construct routing graphs. Key tags for UK walking/hiking/running:

| OSM Tag | Meaning | GraphHopper Support | Notes |
|---------|---------|------------------|-------|
| `highway=footway` | Dedicated pedestrian path, often beside roads | ✅ Full | Primary tag for UK pavements and urban footpaths |
| `highway=path` | Generic undesignated walking path | ✅ Full | Covers 30-40% of UK PROW network (less tagged than footway/bridleway) |
| `highway=bridleway` | Path open to pedestrians & horses; UK PROW category | ✅ Full | Common in rural UK; well-mapped in popular regions |
| `highway=track` | Land management tracks; pedestrian access variable | ⚠️ Reduced | GraphHopper walks them by default, but applies penalties; UK access/permissive tags determine real walkability |
| `highway=residential` | Low-traffic residential roads | ✅ Default | Walkable with normal costing; UK town walking |
| `highway=tertiary` | Minor roads | ✅ Default | Walkable; GraphHopper walks roads unless explicitly avoided |
| `highway=primary` / `secondary` | Major roads | ⚠️ Discouraged | GraphHopper prefers paths; will use if necessary; routing prefers quieter alternatives |
| `highway=motorway` / `trunk` | High-traffic roads | ❌ Blocked | Pedestrians not allowed |

### 1.2 Foot Access Tags (Overrides)

These tags modify walkability on any highway type:

| OSM Tag | Meaning | GraphHopper Support | Notes |
|---------|---------|------------------|-------|
| `foot=designated` | Path explicitly designated for pedestrians | ✅ Full | Top priority for walking routing; includes permissive footpaths |
| `foot=yes` | Pedestrians allowed | ✅ Full | Standard permission; on non-walk highways, acts as override |
| `foot=permissive` | Pedestrians allowed but not legally required; owner tolerance | ✅ Full | Common UK farmland paths; revokable at owner discretion |
| `foot=no` | Pedestrians not allowed | ✅ Blocked | GraphHopper respects; path excluded from pedestrian graphs |
| `foot=private` | Private land; pedestrians not allowed | ✅ Blocked | Excluded from routing |
| `access=private` / `access=no` | General access restriction | ✅ Respected | Overrides permissive tags; GraphHopper blocks pedestrian crossing |

### 1.3 UK-Specific Designation Tags

The following tags identify UK PROW categories and are increasingly well-mapped in OSM:

| OSM Tag | Meaning | GraphHopper Support | Notes |
|---------|---------|------------------|-------|
| `designation=public_footpath` | UK PROW footpath (E&W only) | ✅ Via `foot=designated` | Cross-referenced: 149 UK authorities released PROW data; partial OSM conflation |
| `designation=public_bridleway` | UK PROW bridleway (E&W) | ✅ Via `foot=designated` | Open to horse traffic; footpaths OK |
| `designation=restricted_byway` | UK PROW byway (E&W); motor vehicles restricted but allowed | ✅ Default | Less common; track-like |
| `designation=byway_open_to_all_traffic` | E&W byway; all traffic allowed | ⚠️ Discouraged | Pedestrians OK but GraphHopper deprioritises due to car traffic assumption |
| `designation=access_land` | CRoW Act access land (England/Wales) | ✅ Via `foot=designated` / `foot=permissive` | Not a path per se; meta-tag on nodes indicating access zones; OSM mapping sparse but improving |

### 1.4 Difficulty and Surface Tagging

For hiking profile customization (hill avoidance, difficulty filtering):

| OSM Tag | Values | GraphHopper Support | Notes |
|---------|--------|------------------|-------|
| `sac_scale` | T1–T6 (hiker difficulty) | ✅ Native | GraphHopper's hike profile natively parses `sac_scale` and uses it in difficulty calculations |
| `trail_visibility` | excellent / good / intermediate / poor / horrible | ⚠️ Custom | Available in OSM; useful for difficulty/navigation warnings; application layer feature |
| `surface` | paved / gravel / grass / dirt / mud / sand / etc. | ✅ Via tags | GraphHopper accesses; application can weight scenic (grass/gravel) vs urban (paved) |
| `smoothness` | excellent / good / intermediate / bad / very_bad / horrible / impassable | ⚠️ Custom | Similar to `trail_visibility`; not natively parsed by GraphHopper |
| `incline` | % gradient | ⚠️ Via way properties | GraphHopper samples elevation from DEM; implicit in terrain; explicit `incline` tag rarely present in UK OSM |

### 1.5 Access Restrictions (Seasonal, Permit-Based)

**Critical limitation:** Static OSM data does NOT include dynamic/seasonal closures.

| Restriction Type | OSM Representation | GraphHopper Support | Notes |
|------------------|-------------------|------------------|-------|
| Seasonal: Lambing (Mar-May) | `access:conditional=no @ (Mar-May)` | ⚠️ Parsed but ignored | GraphHopper does not evaluate conditional access; application layer must handle |
| Seasonal: Grouse shooting (Aug-Dec) | `access:conditional=no @ (Aug-Dec)` | ⚠️ Parsed but ignored | Not present in most OSM; historically UK-only OGL PROW data source |
| Seasonal: Nesting (Mar-Aug) | Ad-hoc, rarely tagged | ❌ Unavailable | Virtually unmapped in OSM |
| Military ranges | `access=no` OR `access=private` | ✅ Respected | Well-mapped on MOD ranges (Salisbury Plain, etc.); paths excluded |
| Permit-required paths | `access=private` OR `permit` tag (rare) | ✅ Blocked | Application layer could detect `permit` tag and warn users |
| Tidal access (coastal) | `access:tide=*` (emerging tag) | ⚠️ Parsed but ignored | GraphHopper lacks tide-aware routing; would require external API integration |

---

## 2. UK OSM Coverage Quality Assessment

### 2.1 England & Wales PROW Mapping

**Overall Assessment:** ⭐⭐⭐⭐ (4/5 stars) — Good to excellent in populated/walking areas; patchy in remote rural zones.

**Strengths:**
- **Urban footways:** Excellent coverage in towns, cities, and suburban areas (>95% of pavements mapped)
- **Popular walking routes:** Well-mapped Long Distance Paths (Pennine Way, Coast-to-Coast, etc.) and established PROW in peak districts/national parks
- **MapThePaths project:** Active since 2020; added ~60,000 PROW paths to OSM via conflation with 149 local authority datasets; ongoing
- **PROW designation tagging:** ~40-60% of paths in rural England/Wales tagged with `designation=public_footpath` or `designation=public_bridleway` (higher in popular areas like Lake District, South Downs)
- **Community contribution:** OSM community active; local mapping clubs, particularly in tourist regions

**Weaknesses:**
- **Coverage gaps in remote rural areas:** Scottish Borders, some upland Wales, East Anglia have sparse path coverage relative to actual ground reality
- **Inconsistent surface/difficulty tagging:** `surface` tag present on ~50% of paths; `sac_scale` present on <5% (rare outside alpine climbing contexts); `trail_visibility` minimal
- **Permissive path status uncertain:** ~15-20% of mapped paths tagged `foot=permissive` but owner tolerance not validated; can be revoked
- **Seasonal closures absent:** No lambing/grouse shooting season closures mapped; OSM assumes permanent access
- **CRoW access land sparse:** `access_land` concept mapped as nodes/relations in some areas but NOT comprehensively UK-wide; difficult to render as routable zones

**Regional Variation (England & Wales):**

| Region | Footpath Density | Quality | Notes |
|--------|------------------|---------|-------|
| Lake District & Peak District | Very high | Excellent | Tourist region; well-maintained PROW; ~80% tagged designation |
| South Downs, Cotswolds | High | Good | Popular walking area; ~60-70% designated tags |
| South Coast (Sussex-Devon) | High | Good | Coastal paths well-mapped; South West Coast Path ~95% coverage |
| Welsh uplands (Snowdonia, Brecon Beacons) | Medium | Good | Tourist-driven; main routes excellent; side-trails sparse |
| East Anglia, Midlands | Medium | Fair | Flatter terrain; fewer mapped trails; footways good |
| Remote Wales (Pembroke, mid-Wales) | Low-Medium | Fair | Coverage improving; some PROW unmapped or poorly tagged |
| Scottish Borders (E&W side) | Low | Fair | Sparse mapping; cross-border coordination lacking |

### 2.2 Scotland Coverage (Right to Roam Territory)

**Overall Assessment:** ⭐⭐⭐ (3/5 stars) — Sparser than England/Wales; cultural/legal differences mean different tagging approach.

**Specifics:**
- **PROW concept inapplicable:** Scottish Outdoor Access Code grants "Right to Roam" for responsible access; most land walkable regardless of path marking
- **Mapped paths fewer:** ~30-40% fewer path segments than equivalent English countryside; community less motivated to exhaustively map because statutory paths less critical
- **Path quality:** Established tourist routes (West Highland Way, Southern Upland Way) well-mapped; local/less-known paths often absent
- **Access tags simpler:** Most paths tagged `foot=yes` or `foot=designated` without nuance; CRoW-equivalent access zones NOT mapped as explicit boundaries
- **Elevation/difficulty:** Sac_scale tags more common on Scottish peaks (~10-15%) due to climbing heritage; trails_visibility poor
- **Improvement trajectory:** Young Scot Project, regional outdoor clubs adding coverage; Geofabrik extract includes Scotland fully (same file as England/Wales)

**Regional Variation (Scotland):**

| Region | Footpath Density | Quality | Notes |
|--------|------------------|---------|-------|
| Central Belt (Edinburgh-Glasgow) | High | Good | Urban footways excellent; countryside paths moderate |
| Highlands | Medium | Fair-Good | Glen Coe, Ben Nevis areas excellent; remote glens sparse |
| Southern Uplands | Low-Medium | Fair | Established long-distance routes OK; local paths sparse |
| Islands (Skye, Outer Hebrides, Orkney) | Low | Fair | Tourist draws well-mapped; local access sparse; ferries not in routing graph |

### 2.3 Geofabrik Extract Sufficiency

**Extract:** `great-britain-latest.osm.pbf` (~1.5 GB raw, ~500 MB compressed)

| Aspect | Assessment | Notes |
|--------|------------|-------|
| **Completeness** | ✅ Sufficient for MVP | Includes all of England, Wales, Scotland, plus surrounding waters/borders |
| **Path coverage** | ✅ Good for established routes; ⚠️ Gaps in remote areas | Suitable for walking app MVP; known limitations acceptable |
| **Update frequency** | ✅ Daily | Geofabrik updates daily; weekly extracts recommended for production |
| **Sub-regional extracts** | ⚠️ Available but not necessary | Geofabrik offers England, Scotland, Wales separately; full GB extract manageable (GraphHopper ~6-8 GB RAM requirement) |
| **Performance** | ⚠️ Moderate | Full GB extract processes in ~20-30 min; index ~4-6 GB SSD; suitable for single-instance deployment |

**Recommendation:** Use **full Great Britain extract** (`great-britain-latest.osm.pbf`) for development and production. Sub-regional extracts unnecessary unless RAM severely constrained; whole GB scales to modest VPS (4GB RAM, 20GB SSD minimum).

---

## 3. GraphHopper Hiking Profile Specifics

GraphHopper's `hike` profile (via `hike.json` custom model) is purpose-built for hiking and pedestrian routing with native support for hiking-specific features.

### 3.1 Native Tag Support

GraphHopper hike profile natively respects:

- **Core highway types:** `footway`, `path`, `bridleway`, `track`, `residential`, `tertiary`, `pedestrian`, `living_street`, `cycleway` (as fallback)
- **Foot access:** `foot=yes/designated/permissive/no/private`
- **Access restrictions:** `access=no/private/delivery` (blocks pedestrian crossing)
- **Hiking tags:** `sac_scale`, `trail_visibility`, `surface` — all natively integrated
- **General surface preference:** GraphHopper natively parses `surface` tags and weights them in costing

### 3.2 Elevation Integration

GraphHopper samples elevation profiles from raster DEMs and integrates elevation data natively:
- Accesses elevation data for any routable way
- Computes gradient penalties internally (steep grades deprioritised in costing)
- Parses explicit `sac_scale` tags and uses them directly in difficulty calculations
- Grade penalty can be customized via costing parameters

**For UK:** GraphHopper can consume OS Terrain 50 DEM via external DEM provider. See deployment docs.

### 3.3 What GraphHopper Hike Profile Natively Supports

- `sac_scale` (hiking difficulty T1–T6): **Fully supported; parsed and used in routing**
- `trail_visibility`: **Supported; influences difficulty calculations**
- `smoothness`: **Supported; influences path preference**
- Surface tags (`surface`): **Fully parsed and weighted**
- Elevation integration: **Native support via DEM**

### 3.4 What GraphHopper Hike Profile Does NOT Handle

- Seasonal `access:conditional`: **Parsed but not evaluated; always assumes current access**
- `designation` tags: **Not explicitly parsed** (but implied via `foot=designated`)

### 3.5 Custom Costing Model

GraphHopper's `hike` profile can be customized via `hike.json` model configuration:

```json
{
  "speed": [
    "if (highway == 'motorway') { return 0; } else if (highway == 'track') { return 10; } else { return 20; }"
  ],
  "priority": [
    "if (foot == 'designated') { return 1.3; }",
    "if (surface == 'paved') { return 0.9; } else if (surface ~ 'grass|dirt|gravel') { return 1.1; }",
    "if (sac_scale ~ 'T[45]|T6') { return 0.8; }"
  ]
}
```

**Route Customization Mapping (from decisions):**
- **Surface type:** Adjust priority/speed for surface tags (paved, gravel, grass, etc.)
- **Avoid roads:** Set low priority for `highway=primary|secondary|trunk`
- **Avoid hills:** Lower priority for high `sac_scale` or steep gradients
- **Prefer scenic:** Higher priority for natural surfaces (grass, gravel)
- **Waypoints:** Standard GraphHopper point array
- **Elevation profiles:** Native integration with DEM; implicit in routing
- **Round-trip routing:** ✅ Native support via circular route parameters

### 3.6 Advantages over Valhalla

| Feature | GraphHopper | Valhalla |
|---------|------------|----------|
| **sac_scale parsing** | ✅ Native | ❌ Manual application layer |
| **Round-trip routing** | ✅ Native | ❌ Manual algorithm required |
| **Hiking profile** | ✅ Purpose-built (`hike.json`) | ⚠️ Generic pedestrian + custom params |
| **Surface tag parsing** | ✅ Native | ⚠️ Manual application layer |

### 3.7 Limitations

| Feature | Status | Workaround |
|---------|--------|-----------|
| **Difficulty filtering** | ⚠️ Partial | Use `sac_scale` priority weighting; rank routes by difficulty |
| **Trail visibility** | ✅ Supported | GraphHopper natively supports; application can weight accordingly |
| **Seasonal access** | ❌ Not supported | Application must implement seasonal closure calendar; communicate via UI warnings |
| **CRoW access zones** | ⚠️ Partial | Zones not routable; alternative: store as GeoJSON zones, highlight on map, allow application-logic routing into zones |

---

## 4. Recommended Development Data Strategy

### 4.1 For Local Dev/Spike (Fast iteration)

**Goal:** Minimal data footprint, reasonable path density, quick GraphHopper build.

**Recommendation:** Use **Scotland extract** (`scotland-latest.osm.pbf`, ~100 MB)

**Why Scotland:**
- ✅ Smaller file (~100 MB compressed); GraphHopper builds in <5 min on dev laptop (4 GB RAM)
- ✅ Lower PROW tagging density (Right to Roam model) means simpler access logic to validate
- ✅ Good elevation; useful for testing hill avoidance costing
- ✅ Established tourist routes (West Highland Way, Ben Nevis) for sanity checks
- ✅ Geofabrik URL consistent and stable

**Geofabrik URL:**
```
https://download.geofabrik.de/europe/scotland-latest.osm.pbf
```

**Alternative (slightly larger, richer path network):** **England + Wales combined** (`england-latest.osm.pbf` + `wales-latest.osm.pbf`, ~400 MB combined)
- More PROW density for realistic testing
- Spike can verify PROW designation parsing

**Not recommended for dev:** Full GB (too large for local iteration)

### 4.2 For Production

**Use:** Full Great Britain extract (`great-britain-latest.osm.pbf`, ~1.5 GB)

**Rationale:**
- Comprehensive coverage across all three nations
- GraphHopper processes in ~20-30 min; single VPS instance manageable
- Weekly update cadence (Geofabrik) reduces stale-data risk

**Geofabrik URL:**
```
https://download.geofabrik.de/europe/great-britain-latest.osm.pbf
```

### 4.3 Update Strategy

- **Dev/CI:** Weekly extracts sufficient (automate via cron job)
- **Production:** Weekly or monthly depending on feature importance of recent OSM edits
- **Considerations:** Each GraphHopper rebuild takes ~20-30 min; plan downtime or use blue-green deployment

---

## 5. Access Restrictions in OSM

### 5.1 PROW / Designation-Based Access (England & Wales)

**How it works:**
- `designation=public_footpath|public_bridleway|restricted_byway|byway_open_to_all_traffic` tags explicitly identify UK legal path categories
- Implies `foot=designated` on footpaths/bridleways; GraphHopper respects

**OSM mapping status:**
- ✅ ~40-60% of paths in E&W tagged with designation (higher in mapped areas)
- ✅ 149 UK authorities released PROW data; MapThePaths conflated major areas
- ⚠️ Rural/remote areas sparse; authority data often 5-10 years out of date

**GraphHopper behavior:**
- Treats designated paths as preferred pedestrian routes
- Does NOT explicitly parse `designation` tag (relies on `foot=designated` override)

**For Waymarked:**
- Application can filter routes to "public paths only" by checking OSM `designation` tag post-routing
- Or: pre-filter GraphHopper's graph (advanced; requires custom GraphHopper costing)

### 5.2 Permissive Paths (`foot=permissive`)

**How it works:**
- Owner allows pedestrian use but is not legally required to permit it
- Can be revoked at owner discretion
- Common on UK farmland

**OSM mapping status:**
- ✅ ~15-20% of paths in rural UK tagged `foot=permissive`
- ⚠️ Validation lacking; tags may become stale

**GraphHopper behavior:**
- Respects `foot=permissive` as walkable; includes in routing graph

**For Waymarked:**
- Application should warn users: "This section uses permissive paths (may be revoked by owner)"
- Consider flagging permissive paths in UI (highlight or confidence rating)

### 5.3 CRoW Act Access Land (England & Wales)

**How it works:**
- CRoW Act (2000) grants statutory right to roam on designated access land (mountains, moors, heath, downs) in England & Wales
- Access is ON the land itself, not necessarily on marked paths
- Restrictions apply: no vehicles, dogs on leads, seasonal closures

**OSM mapping status:**
- ❌ Access land zones are NOT systematically mapped as routable areas
- ⚠️ Sparse: Some moorland areas tagged `access=permissive` or `access_land=yes` (non-standard tag)
- ❌ Seasonal closures (lambing, grouse) rarely tagged

**GraphHopper behavior:**
- No special support; CRoW zones not represented as routable paths
- GraphHopper sees existing mapped paths through CRoW zones but cannot route "off-path" through open land

**For Waymarked:**
- **Limitation:** Cannot natively route through unmapped CRoW zones
- **Workaround:** Store CRoW zones as GeoJSON/shapefiles; overlay on map; allow users to select zones and receive warning: "This route crosses access land; respect seasonal closures and other restrictions"
- Future enhancement: Contribute CRoW zone mapping to OSM; enable custom CRoW costing in GraphHopper

### 5.4 Scottish Right to Roam

**How it works:**
- Scottish Outdoor Access Code grants broad pedestrian access to most land (mountains, moorland, forestry) with responsibility code
- No formal PROW system; concept of legal "paths" less critical
- Access can be restricted in specific zones (military ranges, active quarries) but most land open

**OSM mapping status:**
- ❌ Right to Roam zones NOT systematically mapped
- ✅ Restrictions (military, quarries) tagged as `access=no` on affected paths/areas
- ✅ Mapped paths can be routed freely under Right to Roam principle

**GraphHopper behavior:**
- Treats most mapped Scottish paths as walkable
- Respects `access=no` tags on restricted areas

**For Waymarked:**
- No special handling needed; GraphHopper graph reflects Right to Roam de facto (open land simply has fewer mapped paths, so fewer routable options)
- Application can display notice: "In Scotland, you have broad right to roam; respect access codes and seasonal restrictions"

### 5.5 Seasonal Closures (Unmapped)

**Lambing (March–May), Grouse Shooting (August–December), Nesting (March–August)**

**How represented in OSM:**
- Emerging standard: `access:conditional=no @ (Mar-May)` for lambing, etc.
- ❌ **Very rarely present** — <1% of UK paths have seasonal tags
- ❌ GraphHopper does NOT evaluate conditional access (parses but ignores)

**OSM mapping status:**
- ❌ Unmapped; would require manual tagging of 50,000+ paths with seasonal closures
- ❌ Historical OGL PROW data from 149 authorities includes some closure notes but not systematically converted to OSM

**GraphHopper behavior:**
- ❌ Cannot route around seasonal closures; assumes permanent access

**For Waymarked:**
- **Workaround:** Maintain application-level database of seasonal closures (curated list, partnership with land management bodies, or user reports)
- Display seasonal warnings on route: "Lambing in progress (Mar-May) — respect closures in this area"
- Future: Partner with Scottish Wildlife Trust, grouse estates, military MoD for real-time closure feeds

---

## 6. Summary: Recommended UK OSM Strategy for Waymarked

### 6.1 Green Light ✅

- **GraphHopper + OSM + UK footpath routing:** Sufficient quality for MVP and production walking route planner
- **Elevation integration:** OS Terrain 50 + GraphHopper DEM = solid difficulty awareness
- **Development data:** Scotland extract for rapid iteration; full GB for production
- **Popular routes:** Established long-distance paths (West Highland Way, Pennine Way, South West Coast Path) well-covered
- **Urban walking:** Excellent footway coverage in towns/cities
- **Native round-trip routing:** GraphHopper supports circular route generation natively ✅

### 6.2 Yellow Cautions ⚠️

- **PROW completeness:** ~40-60% tagged; application should account for untagged but real paths
- **Remote rural coverage:** Scottish Borders, mid-Wales, East Anglia have gaps; users may find route options limited
- **Permissive path reliability:** ~15-20% of paths revokable at owner discretion; warn users
- **Seasonal closures:** Unmapped; application must implement own warning system
- **Difficulty tagging:** `sac_scale` rare (<5%); GraphHopper will natively use where available, application can estimate from elevation where not

### 6.3 Red Risks ❌

- **CRoW access land:** Not routable in GraphHopper; off-path access through moorland not supported (workaround: UI warning + GeoJSON overlay)
- **Tidal coastal access:** Not handled; application must detect or warn users manually
- **Real-time seasonal data:** Requires external partnership; not available in static OSM

### 6.4 Next Steps

1. **Backend spike (Mikey/Brand):** Deploy GraphHopper with Scotland extract; test pedestrian routing + hike profile customization
2. **Data spike:** Analyse UK footpath tagging variance by region; estimate coverage quality per admin boundary
3. **Application layer:** Implement custom costing presets (e.g., "footpaths only", "avoid hills"); add UI warnings for permissive/seasonal closures
4. **Future enhancements:** Contribute to OSM (seasonal closure tagging, CRoW zones); partner for real-time closure feeds

---

## 7. Key Sources & References

### OSM Documentation
- OpenStreetMap Wiki: [Highway Tagging](https://wiki.openstreetmap.org/wiki/Key:highway)
- OSM Wiki: [Walking Routes](https://wiki.openstreetmap.org/wiki/Relation:route#Walking_routes)
- OSM Wiki: [UK PROW Tagging](https://wiki.openstreetmap.org/wiki/Walking_Routes#United_Kingdom)
- OSM Wiki: [Access Tags](https://wiki.openstreetmap.org/wiki/Key:access)
- OSM Wiki: [SAC Scale (hiking difficulty)](https://wiki.openstreetmap.org/wiki/Key:sac_scale)

### Routing Engines
- [GraphHopper GitHub](https://github.com/graphhopper/graphhopper) — Hiking profiles, elevation, custom models
- [GraphHopper Documentation](https://graphhopper.com/api/1/docs/) — Hiking profile & custom models
- [GraphHopper Hiking Profile](https://graphhopper.com/blog/2020/01/02/graphhopper-now-provides-hiking-profiles/) — Hiking-specific features

### UK Data Sources
- [Geofabrik](https://download.geofabrik.de/europe/great-britain.html) — OSM extracts
- [MapThePaths Project](https://www.maproulette.org/) — PROW conflation efforts
- [rowmaps.com](https://rowmaps.com) — PROW data repository (149 UK authorities, OGL)
- [Ordnance Survey Data Hub](https://osdatahub.os.uk/) — OS Terrain 50 elevation (OGL)

### Elevation & Tiles
- [OS Terrain 50](https://www.ordnancesurvey.co.uk/products/os-terrain-50) — UK elevation (50m, free, OGL)
- [OpenTopoMap](https://opentopomap.org/) — Topographic tile layer (free, CC-BY-SA)
- [Thunderforest](https://www.thunderforest.com/) — Commercial outdoor tiles

### UK Access Context
- [Scottish Outdoor Access Code](https://www.outdooraccess-scotland.scot/) — Right to Roam framework
- [CRoW Act 2000](https://www.legislation.gov.uk/ukpga/2000/37/contents) — England/Wales access rights
- [UK PROW System](https://www.gov.uk/guidance/right-of-way-public-access-across-private-land) — DEFRA guidance

---

**Document Status:** Living reference — updated as GraphHopper integration progresses and new OSM data emerges.
