# 17 — Backend: distance-to-request on the detail (süre deferred)

The redesigned detail's **Konum** card shows "MESAFE: … / SÜRE: …". The detail returns snapped coords but **no
distance**. This adds `distanceKm` to the provider detail, computed the **same way discovery already does it** —
in the module, in SQL, from the exact coordinates (which never leave the repository). "Süre" (ETA) is deferred; see
§ Duration.

## The architectural rule this must obey (do not break it)

`GeoHelper` carries an explicit exception note:

> `// Keep it isolated: no geo logic in domain entities, in the BFF, or in the SPA.`

So distance is computed **in the ServiceRequest module**, not the BFF and not the browser. The BFF only forwards
the viewer's centre down and carries `distanceKm` up. Discovery already follows this — mirror it exactly.

## Verified in source (2026-07-16)

- Discovery computes `DistanceKm` in SQL via a haversine expression in `ServiceRequestRepository`
  ("Haversine distance computed in SQL (null when no centre)"), from the **exact** `LocationLatitude/Longitude`.
  Only the **snapped** coords + the resulting distance leave the repo — exact coordinates never do.
- `GeoHelper` (module) holds the geo maths; centre comes from the client (browser geolocation), passed as query
  params, exactly like discovery's `centerLatitude/centerLongitude`.
- The provider detail path: `ProviderJobsController.GetServiceRequestDetail(serviceRequestId)` →
  `GetProviderServiceRequestDetailQuery(serviceRequestId)` → handler → `GetProviderServiceRequestDetailResponse`
  (Request DTO carries snapped `ApproxLatitude/Longitude`, **no** `DistanceKm`).
- Distance is **per-viewer** (depends on the provider's location), so it must be computed per request and **never
  cached** with the request. (The BFF caches only the vessel summary, not the request body — good; keep it that way.)

## Work

### 1. ServiceRequest module — compute distance in the detail query
- `GetProviderServiceRequestDetailQuery`: add optional `decimal? CenterLatitude`, `decimal? CenterLongitude`.
- Detail query handler / repository read: when both are present **and** the request has real coordinates, compute
  `DistanceKm` with the **same haversine expression discovery uses** (reuse it — do not write a second copy; if it
  is inline in `ServiceRequestRepository`, extract a small shared expression/helper so both call one definition).
  Exact coords stay in the repo; only `DistanceKm` (rounded, e.g. 1 decimal) is projected out.
- Put `DistanceKm (decimal?)` on the detail Request DTO. Null when no centre, or the request has no coordinates.
- Access check unchanged — this is the same provider-scoped detail; distance is just an extra projected column.

### 2. Controller — accept the centre
`GetServiceRequestDetail` action: add `[FromQuery] decimal? centerLatitude`, `[FromQuery] decimal? centerLongitude`
and pass them into the query. Absent → distance null (today's behaviour).

### 3. BFF — forward the centre, carry the distance (NO geo maths here)
- `IProviderServiceRequestRemoteCall.GetServiceRequestDetail(...)`: add optional `centerLatitude`,
  `centerLongitude` query params.
- `GetServiceRequestDetailBffQuery` + handler + `ProviderServiceRequestsController.GetDetail`: accept optional
  `centerLatitude/centerLongitude` and pass them through to the module call. The BFF must **not** compute distance
  — it only relays. Add `distanceKm` to the BFF detail Request DTO so it reaches the SPA.
- Do not cache anything keyed without the centre in a way that would serve a stale distance; the request body is
  not cached today, so nothing to change — just don't add caching of `distanceKm`.

## Duration ("Süre") — deferred, on purpose
There is no routing/ETA service in the stack (no PostGIS, no OSRM). A straight-line distance is honest; a travel
**time** is not derivable without routing, and a road ETA to a marina is out of MVP scope. Options recorded:
- **MVP (recommended): omit süre.** Return `distanceKm` only; the SPA shows "Mesafe: X km" and hides süre.
- Post-MVP: a real ETA belongs to the future **GeoDiscovery** module (with a routing provider), alongside the geo
  migration `GeoHelper` already anticipates. Do **not** fake it as `distance / assumedSpeed`.

## Acceptance — observed
- `GET /provider/service-requests/9011?centerLatitude=38.32&centerLongitude=26.30` → `distanceKm` present and
  sensible for Çeşme; **without** the centre → `distanceKm` null, everything else unchanged.
- The value matches discovery's distance for the same request + centre (same haversine, no drift).
- Exact coordinates never appear in the response (only snapped `approx*` + `distanceKm`). One SQL pass, no N+1.
- No geo maths added in the BFF or exposed to the SPA.

## Frontend (I will do after this lands)
Pass the provider's browser geolocation (already captured for discovery) to the detail fetch; render
`Mesafe: {distanceKm} km` in the Konum card. No süre until GeoDiscovery.

## Report
Append to `REPORT_BACKEND.md` ("17"): the two detail responses (with/without centre), confirmation the value
matches discovery, and that exact coords never leak. Unfinished is **not done**.
