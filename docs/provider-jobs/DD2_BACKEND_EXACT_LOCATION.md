# DD-2 — Backend: exact (non-snapped) location for the assigned provider (Job Workspace)

Resolves DEV_DEBT DD-2. The Job Workspace map + "Yol Tarifi Al" (directions) currently use **snapped ~500 m** coordinates
(`ApproxLatitude/Longitude`) inherited from the discovery/pre-acceptance privacy model. But the job aggregate is served
**only to the assigned provider** (JD-1 access-checks ownership), and that provider must reach the exact berth — snapping
is no longer appropriate here.

## Verified in source (2026-07-17)
- `ServiceRequestEntity.LocationLatitude / LocationLongitude` are the **exact** stored coordinates.
- `ToProviderDetailDto(...)` (mapping extension) snaps them:
  `ApproxLatitude = SnapCoordinate(entity.LocationLatitude, entity.Id)` (~500 m grid + per-row jitter). Used by BOTH the
  pre-acceptance provider detail AND the job aggregate.
- `GetProviderJobDetailQueryHandler` builds `var detail = sr.ToProviderDetailDto(profileId)` (line ~52) after it has
  already resolved + access-checked that the caller **owns this assignment** (`assignment.ProviderProfileId ==
  providerProfileId`, else "Job not found").

## Work — one handler override (no shared-DTO or FE contract change)
In `GetProviderJobDetailQueryHandler`, **after** `sr.ToProviderDetailDto(profileId)` and the ownership check, overwrite
the request's approx coordinates with the **exact** ones, because this aggregate only ever reaches the assigned provider:

```csharp
// DD-2: the job aggregate is served only to the assigned provider (checked above) — give the exact berth, not the
// discovery-snapped point, so the map + directions land on the vessel.
detail.Request.ApproxLatitude  = sr.LocationLatitude;
detail.Request.ApproxLongitude = sr.LocationLongitude;
```

- Keep `SnapCoordinate` / `ToProviderDetailDto` snapping untouched for every **pre-assignment** surface (discovery,
  markers, the offer-eligible SR detail) — those must stay snapped.
- Do **not** change the shared `ProviderServiceRequestDto` shape; the FE already reads `approxLatitude/approxLongitude`
  and will now receive the exact point. (Semantically the field now carries "the location shown to the assigned
  provider" = exact; documented by the comment.)
- Distance (17) math is unaffected.

## Constraints
- **Access already enforced**: JD-1 rejects a job not owned by the caller before this point, so the exact coordinate is
  only ever exposed to the assigned provider. No other endpoint changes.
- Privacy for discovery/pre-acceptance is unchanged — snapping stays everywhere the provider hasn't won the job yet.

## Acceptance — observed
- `GET /provider/jobs/91001` → `request.approxLatitude/approxLongitude` equal the SR's **exact** `LocationLatitude/
  Longitude` (not a snapped grid point). The Job Workspace map marker + "Yol Tarifi Al" now land on the berth.
- Discovery / pre-acceptance SR detail for the SAME SR (a provider who has NOT won it) still returns the **snapped**
  coordinates.

## Report
Append to `REPORT_BACKEND.md` ("DD-2 exact location"): the job aggregate now returns exact coordinates for the assigned
provider while discovery stays snapped. Then DEV_DEBT DD-2 can be marked resolved.

## Frontend (I do alongside — tiny)
Rename the map label fallback from "Yaklaşık konum" to "Konum" (the point is now exact); the directions button already
uses the coordinates as-is.
