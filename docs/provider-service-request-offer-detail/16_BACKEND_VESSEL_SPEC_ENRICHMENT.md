# 16 — Backend: surface vessel Year / Material / Registry on the detail

The redesigned detail page shows **Model, Yıl, Uzunluk, Materyal** in "Tekne Bilgileri" and a **Tekne Kaydı** row in
"Hızlı Bilgiler". Model + Uzunluk already arrive; **Yıl, Materyal, and Tekne Kaydı are missing** and render as "—".
The data already exists in the Vessel module — it is simply not carried through the summary DTO the BFF enriches
from. This adds three fields end-to-end. Small, additive, no behaviour change.

## Verified in source (2026-07-16)

- `VesselSpecificationEntity` already holds `ProductionYear (int?)` and `HullMaterialCode (string?)`.
- `VesselEntity` already holds `RegistrationNumber (string?)` (+ `HomeMarinaName`, `HomeCityCode`).
- The provider detail enriches vessel data via **one bulk call**:
  `GetServiceRequestDetailBffQueryHandler.EnrichVesselAsync` → `IProviderVesselRemoteCall.GetSummaries(ids)` →
  `/api/v1/vessels/summary` → `GetVesselSummariesResponse { Items: VesselSummaryDto[] }`, cached
  (`vessel:summary:{id}`, 10 min).
- `VesselSummaryDto` today carries only: `VesselId, Name, VesselTypeCode, Brand, Model, LengthValue,
  LengthUnitCode`. **No year, material, or registration.**
- `ApplyVessel(...)` maps those onto `response.Detail.Request.Vessel*`. The detail Request DTO has
  `VesselTypeCode/Brand/Model/LengthValue/LengthUnitCode` but **no** year/material/registration field.

## Work

### 1. Vessel module — widen the summary DTO + its query
`GetVesselSummariesResponse.VesselSummaryDto` (Abstraction): add
```csharp
public int? ProductionYear { get; init; }
public string? HullMaterialCode { get; init; }
public string? RegistrationNumber { get; init; }
```
In the `/api/v1/vessels/summary` query handler, project these from the vessel + its `Specification`
(`ProductionYear`, `HullMaterialCode` from spec; `RegistrationNumber` from the vessel). Keep the "missing ids are
omitted, not errored" contract. The summary must **not** trigger an N+1 — include the specification in the same
query the handler already runs (it already returns Brand/Model/Length from the spec, so the spec is already
joined; add these columns to that projection).

`HullMaterialCode` is a **ReferenceData code**, not a label — do not translate server-side. The SPA maps it to a
label the same way it maps `VesselTypeCode` (i18n `vesselType.*` → add `hullMaterial.*`). Codes stay codes.

### 2. BFF — carry the three fields onto the detail Request DTO
- Detail Request DTO (`GetProviderServiceRequestDetailResponse` Request): add
  `VesselYear (int?)`, `VesselMaterialCode (string?)`, `VesselRegistrationNumber (string?)`.
- `ApplyVessel(...)`: map `vessel.ProductionYear → VesselYear`, `vessel.HullMaterialCode → VesselMaterialCode`,
  `vessel.RegistrationNumber → VesselRegistrationNumber`.
- **Bump the vessel-summary cache key** (e.g. `vessel:summary:v2:{id}`) so the 10-min cache doesn't serve old
  shapes without the new fields after deploy.

### Privacy note (unchanged)
`RegistrationNumber` is a property of the **vessel**, not the owner — it does not identify the customer and is fine
to show to a provider evaluating the job (like the marina name already shown). Do **not** add owner name, phone, or
`ownerUserId` — that boundary stays.

## Acceptance — observed
- `GET /provider/service-requests/9011` returns `vesselYear`, `vesselMaterialCode`, `vesselRegistrationNumber`
  when the vessel has them (Aegean Wind: confirm the values present in its spec/row; if the seed vessel has no
  `ProductionYear`/`HullMaterialCode`, set them on the seed so the screen shows real values).
- Vessels missing these still return the rest (fields null, no error).
- One bulk call, no N+1; cache key bumped. Codes returned raw (no server-side translation).

## Frontend (I will do after this lands)
Render `Yıl`, `Materyal` (via new `hullMaterial.*` i18n) in Tekne Bilgileri, and `Tekne Kaydı`
(`vesselRegistrationNumber`, optionally with `HomeCityCode`) in Hızlı Bilgiler — replacing the current "—"/privacy
placeholders with real data.

## Report
Append to `REPORT_BACKEND.md` ("16"): the detail JSON for SR 9011 showing the three fields, the seed values used,
and the bumped cache key. Unfinished is **not done**.
