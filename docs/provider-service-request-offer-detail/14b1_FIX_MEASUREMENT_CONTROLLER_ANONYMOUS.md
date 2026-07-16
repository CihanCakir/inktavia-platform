# 14b.1 — Fix: 14b does not actually reject unknown units (the real cause)

14b's own test shows `ZZZZ` was **accepted**, not rejected — so the fail-closed rule is not working, and the
whole point of 14b (reject unknown units) is unmet. This closes it. Small, one file. Do not touch the frontend or
the offer logic.

## Root cause (verified in source 2026-07-15)

The report blames "the same inter-module auth gap as city validation." That is wrong: **city validation works
end-to-end** — we proved `BODRUM` rejected and `35` accepted. The reason it works is that `LocationController` is
`[AllowAnonymous]` (we made it so during the discovery work). `MeasurementController` is **not** annotated, so the
BFF-assertion service-token inter-module call gets a 401, the `UnitCodeValidator` hits its "ReferenceData
unavailable → skip" branch, and the unknown unit sails through.

- `Modules/ReferenceData/.../Controller/V1/ReferenceData/LocationController.cs` → class-level `[AllowAnonymous]`.
- `Modules/ReferenceData/.../Controller/V1/ReferenceData/MeasurementController.cs` → no `[AllowAnonymous]` → 401
  on the inter-module call.

Measurement units are the same class of data as countries/cities: **read-only public reference data behind the
network boundary.** They should have the same access as `LocationController`.

## Work

1. Add class-level `[AllowAnonymous]` to `MeasurementController` (read-only), mirroring `LocationController`.
2. Copy the same guard comment `LocationController` carries, adapted:
   ```csharp
   // ⚠️ CLASS-LEVEL [AllowAnonymous] — READ-ONLY CONTROLLER. Do not add a write endpoint here.
   // Measurement units are public reference data (the offer builder validates unit codes against them). These
   // GETs carry no token. [AllowAnonymous] is on the CLASS: any action added below inherits it — a write
   // endpoint dropped in here would be publicly writable. Mutations belong on MeasurementAdminController.
   // "Anonymous" means "no token required inside the cluster", not "exposed to the internet": modules have no
   // public ingress and a NetworkPolicy admits only the BFFs (infrastructure/k8s). That boundary is what makes
   // this safe — if it is removed, this endpoint is genuinely open.
   ```
   (Confirm the admin/write endpoints for measurement units live on `MeasurementAdminController`, not this one.)
3. Do **not** change the `UnitCodeValidator`'s "skip on unavailable" degradation — that is a reasonable last
   resort. The fix is that ReferenceData is now reachable, so the validator gets the real answer and rejects
   `ZZZZ`. (If you want defence in depth, you may log at Warning when the skip branch is hit, so a future outage
   is visible — optional.)

## Acceptance — observed (this is the criterion 14b failed)
- Save a draft with `unitCode: "ZZZZ"` → **rejected `SR_OFFER_UNKNOWN_UNIT`.** Paste it.
- `unitCode: "PIECE"` → accepted. `unitCode` omitted → accepted.
- Same on **submit**, not only save.
- The `UnitCodeValidator` no longer hits its "ReferenceData unavailable" branch for this call (no 401 in the
  logs).

## Constraints
- One controller annotation + comment. No frontend, no offer-logic change, no new endpoint.
- Read-only stays read-only. Do not add write actions under the anonymous class.

## Report
Append to `REPORT_BACKEND.md` (correct the 14b entry): `ZZZZ` is now rejected end-to-end; note the cause was the
missing `[AllowAnonymous]` on `MeasurementController`, not a generic auth gap. Unfinished is **not done**.
