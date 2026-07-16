# 09b — Backend Phase 2: Geospatial filtering & coordinate privacy

Run **after 09a is merged and verified**. Scope: radius, bounding box, distance, snapping, city fallback,
`LocationMode`. Do not touch the frontend; the BFF comes in 09c.

## Architectural exception — state it in the code

Live geo search belongs to the future **GeoDiscovery** module. It is implemented inside ServiceRequest as a
**deliberate, temporary exception** to ship this screen. Put this header on **every** geo file — without it,
"temporary" becomes permanent by silence:

```csharp
// ⚠️ GEO ON LOAN — ARCHITECTURAL EXCEPTION.
// Live geo search belongs to the future GeoDiscovery module (project rule). Kept here to ship provider
// discovery. Keep it isolated: no geo logic in domain entities, in the BFF, or in the SPA.
// Migration triggers: persistent provider coordinates · provider service-area onboarding · polygon service
// areas · geo ranking · more than ~10^5 candidate rows per query.
```

## Decisions already taken (do not relitigate)

- **No PostGIS in this phase.** The database is external/managed (an extension is an ops request, not a migration
  we control), the candidate set is small, and the question is "points in a box, ordered by distance" — not
  geometry.
- **Indexed bounding box → then haversine.** The bbox is computed **server-side** from `(center, radiusKm)`
  (latitude-corrected) and is the indexed prefilter; the exact circle and the distance value are computed **in
  SQL** on the small candidate set.
- **If EF cannot translate the expression, use `FromSqlInterpolated`.** **Materialising rows to compute or sort
  by distance is a failure, not a fallback.** If you find yourself calling `ToList()` before a distance sort,
  stop and report it.

## Work

### 1. Geo filtering
- Extend `ProviderServiceRequestDiscoveryFilter` with: `CenterLatitude`, `CenterLongitude`, `RadiusKm`,
  `BoundsMinLat/MaxLat/MinLng/MaxLng`, and `Sort = DistanceAsc`.
- Index `(Status, LocationLatitude, LocationLongitude)`.
- Return `DistanceKm` (null when there is no origin). Sort `DistanceAsc` with **`Id` as the stable tiebreak** —
  equal distances must not oscillate between pages.

### 2. Browser coordinates are untrusted discovery input
Validate server-side: lat ∈ [-90, 90], lng ∈ [-180, 180]; `RadiusKm` **capped** (≤ 200); bounds well-formed;
`Sort = DistanceAsc` ⇒ center required.

They may **narrow the caller's own view and nothing else**. They are never an authorization input, never an
entitlement input, never service-area validation. A caller cannot see anything with coordinates that they could
not see without them.

### 3. City fallback — a first-class path, not an error path
No coordinates (permission denied, unavailable, or simply not sent) ⇒ filter by
`LocationCityCode == provider profile city`, `DistanceKm = null`, `LocationMode = ProviderCity`.

**Location permission will be refused.** A screen that only works when the user says yes is a screen that breaks
for everyone who says no.

### 4. `LocationMode` — a code, never a label
Return `LocationMode ∈ { BrowserLocation, MapViewport, ProviderCity }` on the list and summary responses.

The backend never returns a display string. And **the phrase "service area" appears nowhere**: we have no
provider service-area data (see doc 00 §1), and naming a thing we cannot compute is how a UI starts lying.

### 5. Coordinate privacy — snapping
Discovery returns **snapped** coordinates: rounded to a ~500 m grid, **deterministic per request id** so a marker
does not jitter between page loads.

Exact `LocationLatitude`/`LocationLongitude` are released **only** to the **assigned** provider, through the
existing job endpoints. A discovery marker means "there is work near this marina" — not "the boat is at berth
D-14". The exact coordinates must not appear in any discovery DTO, log line, or cache entry.

## Acceptance (observations, not claims)

- The **generated SQL** for the discovery query is pasted in the report and shows the bbox predicate **using the
  index**.
- No distance arithmetic anywhere outside the database — not in the handler, not in the BFF, not in the SPA.
- No origin ⇒ city results, `DistanceKm = null`, `LocationMode = ProviderCity`.
- Radius above the cap and out-of-range coordinates are **rejected**, not clamped silently.
- Discovery never returns exact coordinates; snapped values are stable across repeated calls.
- Every geo file carries the **GEO ON LOAN** header.

## Tests

Known-fixture radius query · bounds query · distance ordering (with the `Id` tiebreak) · no-origin → city
fallback + `LocationMode` · snapped coordinates deterministic and ≠ exact · exact coordinates absent from every
discovery projection · validation rejections (range, cap, `DistanceAsc` without center) · cursor stability when
sorting by distance.

## Report

`REPORT_BACKEND_09b.md`: the generated SQL (discovery + markers precursor), the index actually used, the snapping
function and grid size, and **every place you were tempted to compute in memory**. If something could not be
done, it is **not done** — not "done with open items".
