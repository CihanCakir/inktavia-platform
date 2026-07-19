# JD-4 — Backend: widen vessel summary with Beam / Draft

The Job Workspace "Hızlı Bilgiler" (and the request detail vessel card) show Boy (length) + Yıl + Materyal, but not
**En (Beam)** / **Draft** — the design asks for them and the spec already stores them. This phase widens the vessel
summary DTO + projection (the same widening done earlier for Year/Material, #16) and bumps the cache key. Cheap, additive,
no behavior change.

## Verified in source (2026-07-17)
- `VesselSpecificationEntity` **already has** `BeamValue`, `BeamUnitCode`, `DraftValue`, `DraftUnitCode`.
- `VesselSummaryDto` currently exposes: `VesselId, Name, VesselTypeCode, Brand, Model, LengthValue, LengthUnitCode,
  ProductionYear, HullMaterialCode, RegistrationNumber` — **no Beam/Draft**.
- `GetVesselSummariesQueryHandler` projects the DTO from `VesselSpecifications` (left join, `DefaultIfEmpty`). Add the four
  fields there.
- Cache key `vessel:summary:v2:` appears in **4 places** (stale v2 entries would lack Beam/Draft): `ProviderJobsController`
  (BFF, job-detail enrichment, inline `$"vessel:summary:v2:{VesselId}"`), `GetProviderJobsQueryHandler`,
  `GetServiceRequestDetailBffQueryHandler`, `GetProviderDiscoveryBffQueryHandler`. Bump all to `v3`.
- The BFF enriches `ProviderServiceRequestDto.Vessel*` from the summary (job detail controller maps
  `VesselYear/MaterialCode/…`). `ProviderServiceRequestDto` has no Beam/Draft fields yet → add them as the enrichment
  target.

## Work

### 1. Vessel module — widen the summary
- Add to `VesselSummaryDto`: `BeamValue (decimal?)`, `BeamUnitCode (string?)`, `DraftValue (decimal?)`,
  `DraftUnitCode (string?)`.
- In `GetVesselSummariesQueryHandler` projection, map them from `spec` (guard `spec != null`, same pattern as
  `LengthValue`).

### 2. Bump the vessel summary cache key v2 → v3 (all 4 sites)
Change `vessel:summary:v2:` → `vessel:summary:v3:` in the BFF job-detail controller inline key,
`GetProviderJobsQueryHandler`, `GetServiceRequestDetailBffQueryHandler`, `GetProviderDiscoveryBffQueryHandler`. (A new key
avoids serving cached v2 payloads that lack Beam/Draft.)

### 3. ServiceRequest module — enrichment target fields
Add to `ProviderServiceRequestDto`: `VesselBeamValue (decimal?)`, `VesselBeamUnitCode (string?)`,
`VesselDraftValue (decimal?)`, `VesselDraftUnitCode (string?)`. Nullable, additive — render only what's present.

### 4. BFF — map the new fields where vessel enrichment runs
In the **job detail** enrichment (`ProviderJobsController.GetJobDetail`) and the **SR detail** BFF handler
(`GetServiceRequestDetailBffQueryHandler`), after resolving `vessel`, set:
`req.VesselBeamValue = vessel.BeamValue; req.VesselBeamUnitCode = vessel.BeamUnitCode; req.VesselDraftValue =
vessel.DraftValue; req.VesselDraftUnitCode = vessel.DraftUnitCode;` (same block that already maps Year/Material).

## Constraints
- Additive + nullable only — no contract break; existing consumers unaffected. Codes stay codes (unit codes localized by
  SPA). Money none. One bulk vessel call unchanged (just more columns). No customer PII.

## Acceptance — observed
- `GET /provider/jobs/91001` → `request.vesselBeamValue` / `vesselDraftValue` (+ unit codes) populated when the vessel
  spec has them (Aegean Wind); null when absent (render nothing). Length/Year/Material still present.
- A cold cache (new v3 key) returns the widened summary; no stale v2 payload served.

## Report
Append to `REPORT_BACKEND.md` ("JD-4"): the widened summary fields + the four v3 cache-key bumps, and the job-detail
response now carrying Beam/Draft for a spec that has them. Unfinished is **not done**.

## Frontend (I build after this lands — FE-C quick-info widening)
Add Beam/Draft to `JobDetailRequest` + render "En" / "Draft" rows in the Job Workspace "Hızlı Bilgiler" card (and the SR
detail vessel card), only when present.
