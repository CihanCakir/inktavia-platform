# 14a — Backend: finish the vessel enrichment on the detail page

Small, self-contained BFF fix. Do not touch the frontend, the offer domain, or anything else.

## The gap

The detail page's "Tekne Bilgileri" panel can only show the vessel **name**. `GetServiceRequestDetailBffQueryHandler`
already fetches the full `VesselSummaryDto` (one cached bulk call) — which carries `VesselTypeCode`, `Brand`,
`Model`, `LengthValue`, `LengthUnitCode` — but `ApplyVessel(...)` throws all of that away and sets only
`response.Detail.Request.VesselName`. So the enrichment does the expensive part (the call) and discards the
result.

Verified in source (2026-07-15):
- `Modules/Vessel/.../Response/Vessel/GetVesselSummariesResponse.cs` → `VesselSummaryDto`: `VesselId`, `Name`,
  `VesselTypeCode`, `Brand`, `Model`, `LengthValue`, `LengthUnitCode`.
- `Modules/ServiceRequest/.../Dto/ProviderServiceRequestDto.cs` has only `VesselId` + `VesselName`.
- `GetServiceRequestDetailBffQueryHandler.ApplyVessel` sets only `VesselName`.

## Work

1. Add nullable vessel-spec fields to `ProviderServiceRequestDto`: `VesselTypeCode` (`string?`), `VesselBrand`
   (`string?`), `VesselModel` (`string?`), `VesselLengthValue` (`decimal?`), `VesselLengthUnitCode` (`string?`).
   The module leaves these null (there is no vessel snapshot on the request — that was removed in 09b.1); the BFF
   fills them. Do not re-introduce a vessel snapshot on the entity.
2. In `ApplyVessel`, set them from the fetched `VesselSummaryDto`:
   ```csharp
   response.Detail.Request.VesselName ??= vessel.Name;
   response.Detail.Request.VesselTypeCode = vessel.VesselTypeCode;
   response.Detail.Request.VesselBrand = vessel.Brand;
   response.Detail.Request.VesselModel = vessel.Model;
   response.Detail.Request.VesselLengthValue = vessel.LengthValue;
   response.Detail.Request.VesselLengthUnitCode = vessel.LengthUnitCode;
   ```
3. This stays **one** cached bulk call per detail (unchanged) — no new call, no N+1.

## Constraints
- BFF enrichment only. No module domain change, no migration, no snapshot on the entity.
- Codes stay codes (`VesselTypeCode`, `LengthUnitCode`) — the frontend translates. No display strings from the
  backend.
- Nothing else in the detail response changes.

## Acceptance — observed
- `GET /provider/service-requests/{id}/detail` for a request whose vessel exists returns non-null
  `vesselTypeCode` / `vesselBrand` / `vesselModel` / `vesselLengthValue`. Paste the vessel block.
- A request whose vessel does not resolve still returns (fields null) — the detail must not fail because a vessel
  is missing (keep the existing try/catch that logs and continues).
- Still one vessel call per detail.

## Report
Append to `REPORT_BACKEND.md` (section "14a"): the vessel block from a real detail response, and confirmation it
is still one call. If the test request's vessel does not exist in the Vessel DB, say so and note that a vessel
seed is needed to see specs end-to-end.
