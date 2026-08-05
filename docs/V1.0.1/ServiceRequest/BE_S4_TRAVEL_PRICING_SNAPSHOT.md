# BE_S4 — travel / mobilization pricing (structured) + immutable travel snapshot

> **Repo:** `addesso-project` — **ServiceRequest module** (+ the Payment acceptance snapshot; reuses the R3 KILOMETER unit
> via the existing ReferenceData remote call). SR second-wave phase S4 (§20.8). Structures the **travel / mobilization**
> cost a provider charges to reach the vessel: a **flat mobilization fee** or a **per-km × distance** derivation, recorded
> on the `Travel` offer line and **snapshotted immutably at acceptance** — filling the **reserved `TravelPricingSnapshot`
> slot** on `PaymentEconomicsSnapshotEntity`. **Descriptive metadata** — the Travel line's money is already priced by the
> standard line math (it's an Exempt pass-through line in the 8-equality); S4 records *how* it was derived and validates
> consistency. **No money-math change.**

## Scope decision (read first — geo distance is deferred)
Real distance/radius computation belongs to **GeoDiscovery**, which is deferred (supply-side geo data doesn't exist yet;
city-level matching is the MVP — `geodiscovery_deferred`). So in S4 the **distance is provider-declared** (the provider
enters the km, or picks a flat fee); we do **not** compute geo distance. The I2 provider-eligibility read-model supplies
the **origin city** (provider) and the vessel's **destination city** as display/context only. When GeoDiscovery lands, a
computed-distance source can back the same snapshot fields without a schema change.

## Current state (investigated)
- **`ServiceRequestOfferItemType.Travel = 10`** exists, treated as an **Exempt pass-through** line
  (`LineCommissionEligibility.Exempt` + `LineDiscountEligibility.Exempt`) — it enters the 8-equality as a non-commissionable
  line already. **Don't change that.**
- **`PricingMethod.PerKm = 5`** exists; **R3 seeded the KILOMETER unit** (resolve via the remote call
  `GetMeasurementUnitByCode`, added in R3). Line money for any method is `LineSubtotal = Round(Quantity × UnitPrice)` — so a
  per-km travel line is naturally `Quantity = km`, `UnitPrice = perKmRate`.
- **`PaymentEconomicsSnapshotEntity`** explicitly **reserves** the slot: `// S4/S2: TravelPricingSnapshot /
  PricingAttributeSnapshot are RESERVED`. S2 filled the attribute slot; **S4 fills `TravelPricingSnapshot`** the same way
  (immutable child, FK Restrict, tamper guard, no mutators).
- No travel-pricing entity exists yet — build it.

## S4a — structured travel detail on the Travel offer line (capture + validate)
- Add a structured `TravelPricingDetail` attached to a **Travel** offer line (one per Travel line):
  `TravelPricingMethod { FlatMobilization, PerKm }`, `originCityCode?`, `destinationCityCode?` (the vessel's city),
  `distanceKm?` (provider-declared), `perKmRate?` (in the line currency), `unitCode` (KILOMETER for PerKm). Keep it on/beside
  the offer item (a child row or typed columns on the Travel line — match how S2 line values are stored).
- **Validation (fail-loud, `SR_TRAVEL_*` codes):**
  - method **PerKm** → `distanceKm > 0`, `perKmRate > 0`, `unitCode` resolves to the R3 **KILOMETER** unit (via the remote
    call), and **consistency with the line math**: the line's `Quantity == distanceKm` and `UnitPrice == perKmRate`
    (so the snapshot can never disagree with the money). `PricingMethod` on the line should be `PerKm`.
  - method **FlatMobilization** → no km/rate; the line is a fixed amount (`Quantity = 1`, `UnitPrice = fee`); `PricingMethod`
    `Fixed`.
  - only allowed on `ItemType == Travel`; reject travel detail on a non-Travel line.
- This is **descriptive** — it does **not** re-price the line; it records the derivation and refuses an inconsistent one.

## S4b — immutable `TravelPricingSnapshotEntity` at acceptance (fills the reserved slot)
- Add `TravelPricingSnapshotEntity` as an **immutable child of `PaymentEconomicsSnapshotEntity`** (mirror the S2d /
  OfferLine snapshot discipline: FK → snapshot **Restrict**, validating factory, **no mutators**, **tamper guard**).
  Capture per Travel line: `method`, `originCityCode` + **denormalized origin city label**, `destinationCityCode` + label,
  `distanceKm`, `perKmRate`, `unitCode`, and the **resolved travel amount** (= the Travel line total). Self-contained — no
  re-lookup after acceptance (§20.15).
- Populate it in the **P8 acceptance path (`CreateFromLines`)** alongside the other line snapshots, threading the travel
  fields SR→Payment through the existing `CalculateServiceRequestEconomics` remote-call request (additive — same mechanism
  S2d/S3c used). **Enters no sum or invariant** — the Travel line amount is already in the economics as an Exempt line; the
  snapshot is metadata beside it. Tamper guard asserts the resolved amount matches the line (PerKm: `Round(distanceKm ×
  perKmRate)`), else throw.

## S4c — context from I2 (display only)
- Use the I2 provider-eligibility read-model to surface the provider's **origin city** and the vessel's **destination
  city** for the FE picker/preview and to prefill `originCityCode`/`destinationCityCode`. **No distance compute** — the km
  is provider-entered. City codes validate against ReferenceData (the existing `GetCity` remote call).

## Don't-break / QA
- **Additive:** new `TravelPricingDetail` on the Travel line + `TravelPricingSnapshotEntity` + travel fields on the
  economics request DTO + the acceptance-snapshot write. Existing offer/line economics (S1/S6/S7/S8), P8 acceptance, the
  Travel line's Exempt treatment, and the **8-equality invariants are unchanged** — S4 is descriptive (the Travel line is
  priced by the standard line math exactly as today). An offer with no Travel line is byte-identical.
- Migration applies cleanly; travel columns nullable/back-compat. KILOMETER unit resolved via the R3 remote call (cacheable
  per convention). UTC-safe where timestamps appear. Fail-loud on inconsistency. Builds clean.
- Unit tests: (1) no-Travel-line offer → economics + 8-equality byte-identical (regression guard); (2) PerKm travel line
  (km × rate) → detail validates, line math unchanged, acceptance writes the travel snapshot with the resolved amount;
  (3) FlatMobilization → validates, snapshot captures the flat fee; (4) inconsistency (`Quantity != distanceKm` or
  `UnitPrice != perKmRate`, or km on a Flat method, or travel detail on a non-Travel line, or non-KILOMETER unit) →
  `SR_TRAVEL_*` fail-loud; (5) acceptance snapshot immutable + tamper → throw; a later ReferenceData/city change does not
  move the accepted amount.

## Verify
1. Provider adds a **Travel** line priced PerKm (e.g. 40 km × ₺25) → the travel detail validates against the line math and
   the KILOMETER unit; the offer economics is unchanged (Travel stays an Exempt pass-through line).
2. A **FlatMobilization** travel line (e.g. ₺750) validates and records the flat fee.
3. Inconsistent input (km ≠ line quantity, or travel detail on a Service line, or a non-KILOMETER unit) → `SR_TRAVEL_*`.
4. On acceptance, the `TravelPricingSnapshot` is written immutably with origin/destination city labels + distance + rate +
   resolved amount; tampering throws; changing the rate/city in ReferenceData afterward doesn't move the accepted amount.
5. An offer with no Travel line + the full 8-equality suite are unchanged.

## Report
`docs/V1.0.1/ServiceRequest/REPORT_S4.md`: the `TravelPricingDetail` model + validation, the immutable
`TravelPricingSnapshot` (reserved slot filled), the I2 origin/destination context (distance provider-declared, geo compute
deferred), the regression proof that no-Travel offers + the 8-equality are unchanged, and the test results. Then FE
(provider travel-line picker + admin visibility) + next SR phase (S5 part terms).
