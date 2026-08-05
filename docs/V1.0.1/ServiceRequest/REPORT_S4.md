# REPORT_S4 — travel / mobilization pricing (structured) + immutable travel snapshot

**Spec:** `docs/V1.0.1/ServiceRequest/BE_S4_TRAVEL_PRICING_SNAPSHOT.md` — SR second-wave phase S4 (§20.8).
**Modules:** ServiceRequest (structured travel detail on the Travel line + validation + acceptance resolve) + Payment (immutable
acceptance snapshot filling the reserved slot, threaded through the existing `CalculateServiceRequestEconomics` remote-call
contract, exactly as S2d threaded attributes / S3c threaded FX).
**Status:** code complete, both host projects build clean (0 errors), unit tests green (**SR 85/85**, **Payment 285/285**), both
migrations apply clean from scratch on throwaway DBs (schema + FK verified, `inktavia_store` untouched). **NOT committed.** Live
HTTP not exercised (no running stack driven in this session).

**Descriptive, not money-math.** The Travel line (`ServiceRequestOfferItemType.Travel = 10`) is already an **Exempt** pass-through
(`LineCommissionEligibility.Exempt` + `LineDiscountEligibility.Exempt`) priced by the standard line math `LineSubtotal =
Round(Quantity × UnitPrice)`. S4 records **how** that amount was derived and **validates consistency** with the line — it never
re-prices the line, and it enters **no sum and none of the 8 §20.15 equalities**. Distance is **provider-declared** (geo compute
deferred to GeoDiscovery — `geodiscovery_deferred`); S4 computes no distance.

---

## 1. S4a — structured `TravelPricingDetail` on the Travel line (capture + validate)

- **Enum** `Abstraction/Enum/TravelPricingMethod.cs`: `{ FlatMobilization = 1, PerKm = 2 }`.
- **Entity** `Domain/Entities/Pricing/TravelPricingDetailEntity.cs` — a child row bound to the offer line by `OfferItemId`
  (mirrors the S2 `PricingAttributeValueEntity` storage: private ctor + static `Create`/`SetValue`, codes normalised
  upper-invariant). Fields: `Method`, `OriginCityCode?`, `DestinationCityCode?`, `DistanceKm?`, `PerKmRate?`, `UnitCode?`.
  **At most one detail per line** (unique index on `OfferItemId`).
- **Provider write/read** (`Controller/V1/Travel/ProviderTravelPricingController.cs`, `api/v1/service-requests/provider`,
  `[Authorize]`):
  - `PUT offers/{offerId}/items/{itemId}/travel-pricing` — upsert the line's detail (provider-owned offer only; rejected once
    the offer is Accepted/Rejected/Withdrawn/Expired).
  - `GET offers/{offerId}/items/{itemId}/travel-pricing` — the current detail (null when none).
- **Validation** (`Application/Services/Travel/TravelPricingValidator.cs`) — an infra wrapper that resolves the external facts
  fail-loud (the **KILOMETER** unit via the new R3 by-code remote call; the origin/destination city codes via `GetCity`), then
  delegates to the **pure** `TravelPricingValidation.Validate(...)` core (no I/O — unit-tested with plain values). Fail-loud
  `SR_TRAVEL_*` codes (thrown inline, per the module convention — there is no central SR error-code file):

  | Rule | Code |
  |---|---|
  | detail on a non-Travel line | `SR_TRAVEL_NOT_TRAVEL_LINE` |
  | PerKm distance ≤ 0 / rate ≤ 0 | `SR_TRAVEL_DISTANCE_REQUIRED` / `SR_TRAVEL_RATE_REQUIRED` |
  | PerKm unit not the active KILOMETER unit | `SR_TRAVEL_UNIT_NOT_KILOMETER` |
  | PerKm line `Quantity != distanceKm` / `UnitPrice != perKmRate` | `SR_TRAVEL_QUANTITY_MISMATCH` / `SR_TRAVEL_RATE_MISMATCH` |
  | PerKm line `PricingMethod != PerKm` / Flat line `PricingMethod != Fixed` | `SR_TRAVEL_METHOD_MISMATCH` |
  | Flat carries km / rate / unit | `SR_TRAVEL_FLAT_NO_KM` / `_NO_RATE` / `_NO_UNIT` |
  | Flat line `Quantity != 1` / non-positive fee | `SR_TRAVEL_FLAT_QUANTITY` / `SR_TRAVEL_FLAT_FEE_REQUIRED` |
  | unknown origin/destination city | `SR_TRAVEL_UNKNOWN_ORIGIN_CITY` / `SR_TRAVEL_UNKNOWN_DESTINATION_CITY` |

  The **consistency** rules (`Quantity == distanceKm`, `UnitPrice == perKmRate`, method↔PricingMethod) are the key defence: the
  snapshot can never disagree with the money because the detail is refused when it does. This is **descriptive** — it does not
  re-price the line.

## 2. S4c — R3 KILOMETER + city context (display only)

- The R3 by-code endpoint (`GET /api/v1/reference-data/measurement-units/by-code/{code}`, distributed-cached 24 h in the module)
  was **not yet exposed** on the SR side, so `IServiceRequestReferenceDataRemoteCall` gained
  `GetMeasurementUnitByCode(code)` (mirrors `GetCity`) + `SrMeasurementUnitDetailDto{ Code, IsActive }`. A PerKm `unitCode`
  validates when it resolves to an **active** unit whose `Code == "KILOMETER"`. Cacheable per convention (the module query is the
  cache; SR calls it directly like `GetCity`).
- **City codes** validate via the existing `GetCity` remote call (country defaulted `TR`, active-required, fail-loud). `GetCity`'s
  DTO gained an additive `Name` (the ReferenceData `CityDto` already carries it) so the acceptance snapshot can denormalise the
  **city label**.
- **Origin (provider) / destination (vessel) surfacing** for the FE picker rides the **existing** read-models — I2
  `GetProvidersForArea` (provider origin = Identity `UserProfile.City`) + Vessel `HomeCityCode` (destination). Backend obligation
  is city-code **validity** (via `GetCity`) — met above. **No distance compute** (km is provider-entered).

## 3. S4b — immutable `TravelPricingSnapshot` at acceptance (fills the reserved slot)

- **`TravelPricingSnapshotEntity`** (Payment `Domain/Entities/Economics/`) is a **direct child of the aggregate**
  `PaymentEconomicsSnapshotEntity`, finally filling the literal reserved slot (the comment
  `// S4/S2: TravelPricingSnapshot … RESERVED` is now realised; the S2 attribute slot stays at the line level as before). It
  mirrors the S2d/S3c discipline: FK → the aggregate snapshot **OnDelete Restrict**, a single validating `Create` factory,
  **private setters, no mutators**, and a **tamper guard**. Per Travel line it captures `LineRef`, `Method`, `OriginCityCode` +
  **denormalized `OriginCityLabel`**, `DestinationCityCode` + label, `DistanceKm`, `PerKmRate`, `UnitCode`, and the **resolved
  travel amount** (= the Travel line total). Self-contained — **no re-lookup after acceptance** (§20.15).
- **Tamper guard** (mirrors S3c `ApplyFx`): PerKm asserts a positive distance/rate, the KILOMETER unit, and
  `ResolvedTravelAmount == Round(DistanceKm × PerKmRate)`; FlatMobilization asserts no km/rate/unit and a positive fee — so a
  persisted row can never silently disagree with its own numbers. Throws `PaymentEconomicsInvariantException`.
- **Population path** (additive, same mechanism as S2d/S3c): at acceptance
  `AcceptServiceRequestOfferCommandHandler` calls the new `TravelPricingSnapshotResolver` — it loads each Travel line's
  `TravelPricingDetail`, resolves the origin/destination **city labels** via `GetCity`, and emits per-line
  `CalculateServiceRequestEconomicsTravelDto`s (new additive `Travel` on the line DTO). These ride the existing
  `CalculateServiceRequestEconomics` request → `ServiceRequestEconomicsLine.Travel` → `LineEconomicsInput.Travel` → materialised
  inside `CreateFromLines`, where the **resolved amount is set to the Payment-computed line gross** (`= the Travel line total`)
  and the factory tamper-checks it against the SR-declared `Round(km × rate)`.
- **Enters no sum or invariant.** The Travel line amount is already an Exempt line in the economics; the snapshot is metadata
  beside it, deliberately **not** added to any accumulator and untouched by the 8 §20.15 equalities. `CreateFromLines` stays
  pure (label resolution happens upstream, SR-side, as S2d does).

## 4. Don't-break / regression proof

- **Additive:** new `TravelPricingDetailEntity` + table + one remote method + `SrCityValidationDto.Name`; new
  `TravelPricingSnapshotEntity` + table + nullable `Travel` DTO on the economics request + the acceptance-snapshot write.
  Existing offer/line economics (S1/S6/S7/S8), P8 acceptance, the Travel line's Exempt treatment, and the **8-equality invariants
  are unchanged** — S4 is descriptive.
- **No-Travel = byte-identical:** proven by unit test (`Travel_DoesNotChange_TheAggregateEconomics` — a Travel line with the
  snapshot vs without produces byte-identical aggregate economics and **zero** travel snapshot rows; canonical values
  CustomerTotal 6174 / ProviderNet 5400 hold). The full S8 8-equality suite + the S2d attribute + S3c FX suites stay green
  (Payment **285/285**, up from 277, +8 new; none changed). SR **85/85** (up from 73, +12 new).
- **Migrations clean & back-compat:** every new column is nullable except the FK/`LineRef`/`Method`/`ResolvedTravelAmount` on a
  brand-new table (no existing rows). Both chains applied from scratch on throwaway DBs `sr_migtest_s4` / `pay_migtest_s4`
  (dropped after; `inktavia_store` untouched). Verified: SR `travel_pricing_details` with **UNIQUE(`OfferItemId`)**; Payment
  `travel_pricing_snapshots` with FK → `payment_economics_snapshots` **ON DELETE RESTRICT** + the denormalized city-label columns.
  - SR `20260805215758_AddTravelPricingDetails`
  - Payment `20260805215813_AddTravelPricingSnapshots`
- **Cacheable KILOMETER per convention:** the R3 by-code query is distributed-cached (24 h) in ReferenceData; SR calls it
  directly (like `GetCity`). **UTC-safe:** no new timestamps are introduced by S4 (audit timestamps use the existing
  `timestamptz` normalisation). **Fail-loud** on every inconsistency (`SR_TRAVEL_*`) and on a tampered snapshot
  (`PaymentEconomicsInvariantException`).

## 5. Test results

**SR `Aizen.Modules.ServiceRequest.Application.UnitTests` — 85/85** (73 prior + 12 new `TravelPricingValidationTests`):
PerKm (40 km × ₺25) consistent → passes; FlatMobilization (₺750) consistent → passes; and each fail-loud path
(non-Travel line, quantity/rate/method mismatch, non-KILOMETER unit, non-positive distance, km/rate on Flat, Flat quantity ≠ 1).

**Payment `Aizen.Modules.Payment.Domain.UnitTests` — 285/285** (277 prior + 8 new `TravelPricingSnapshotTests`):
1. `CreateFromLines` captures the PerKm snapshot (40 km × ₺25 → `ResolvedTravelAmount` 1000, KILOMETER, denormalized city labels).
2. Captures the FlatMobilization snapshot (₺750, no km/rate/unit).
3. Tamper → throw: line gross 999 vs declared 40 × 25 = 1000 rejected (`*ResolvedTravelAmount*`).
4. Factory guards: Flat-with-distance and PerKm-non-KILOMETER rejected.
5. Travel is descriptive: aggregate economics byte-identical with vs without the snapshot (and 0 rows without).
6. Immutability: no public setters on the S4 snapshot (reflection).
7. **Acceptance freeze:** the accepted snapshot holds the accepted amount; a later rate re-run is a *distinct* object (all setters
   private, no `Update`) — the accepted amount never moves.

**Build:** ServiceRequest host + Payment host build with **0 errors** (pre-existing warnings only).

## 6. Verify (manual, when a stack is running)

1. Provider adds a **Travel** line priced PerKm (40 km × ₺25) and sets the travel detail → validates against the line math and the
   KILOMETER unit; the offer economics is unchanged (Travel stays an Exempt pass-through). ✅ (validation + no-op economics proven
   by unit tests; live HTTP pending a running stack.)
2. A **FlatMobilization** travel line (₺750) validates and records the flat fee. ✅
3. Inconsistent input (km ≠ line quantity, travel detail on a Service line, non-KILOMETER unit) → `SR_TRAVEL_*`. ✅
4. On acceptance, the `TravelPricingSnapshot` is written immutably with origin/destination city labels + distance + rate +
   resolved amount; tampering throws; a later rate/city change in ReferenceData does not move the accepted amount. ✅
5. An offer with **no Travel line** + the full 8-equality suite are unchanged. ✅

## 7. Files touched

**ServiceRequest — Abstraction:** `Enum/TravelPricingMethod.cs` (new), `Request/Travel/SetOfferLineTravelPricingRequest.cs` (new),
`Dto/Travel/TravelPricingDetailDto.cs` (new), `RemoteCall/IServiceRequestReferenceDataRemoteCall.cs`
(`GetMeasurementUnitByCode` + `SrMeasurementUnitDetailDto`; `SrCityValidationDto.Name`).
**ServiceRequest — Domain:** `Entities/Pricing/TravelPricingDetailEntity.cs` (new).
**ServiceRequest — Application:** `Services/Travel/TravelPricingValidator.cs` (+ pure `TravelPricingValidation`) (new),
`Services/Travel/TravelPricingSnapshotResolver.cs` (new), `Command/Travel/OfferLineTravelPricingCommands.cs` +
`OfferLineTravelPricingHandlers.cs` (new), `Command/Offer/AcceptServiceRequestOffer/AcceptServiceRequestOfferCommandHandler.cs`
(travel resolve + threading into `BuildEconomicsRequest`).
**ServiceRequest — Repository:** `Persistence/Configurations/TravelPricingConfigurations.cs` (new),
`Persistence/ServiceRequestDbContext.cs` (DbSet), migration `20260805215758_AddTravelPricingDetails`.
**ServiceRequest — host:** `Controller/V1/Travel/ProviderTravelPricingController.cs` (new), `Program.cs` DI (validator + resolver).
**Payment — Abstraction:** `RemoteCall/Requests/CalculateServiceRequestEconomicsRemoteCallRequest.cs`
(line `Travel` + `CalculateServiceRequestEconomicsTravelDto`).
**Payment — Domain:** `Entities/Economics/TravelPricingSnapshotEntity.cs` (new), `LineEconomicsInput.cs`
(`TravelSnapshotInput` + `Travel`), `ServiceRequestEconomicsCombiner.cs` (thread `Travel`), `PaymentEconomicsSnapshotEntity.cs`
(`_travelPricingSnapshots` child + populate in `CreateFromLines` + reserved-comment update).
**Payment — Application:** `Services/ServiceRequestPaymentEconomicsCalculationService.cs` (DTO→domain `Travel`).
**Payment — Repository:** `Persistence/Configurations/LineEconomicsSnapshotConfigurations.cs` (`TravelPricingSnapshotConfiguration`),
`Persistence/Configurations/PaymentEconomicsSnapshotConfiguration.cs` (FK Restrict + field access), `Persistence/PaymentDbContext.cs`
(DbSet), migration `20260805215813_AddTravelPricingSnapshots`.
**Tests:** `TravelPricingValidationTests.cs` (SR), `Economics/TravelPricingSnapshotTests.cs` (Payment).

## 8. Next

FE (provider travel-line picker driven by the R3 KILOMETER + I2 origin / vessel destination cities; admin visibility of the
travel snapshot). Then the next SR phase: **S5 part terms**.
