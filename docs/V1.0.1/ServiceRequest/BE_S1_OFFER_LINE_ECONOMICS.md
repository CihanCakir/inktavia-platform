# BE-S1 — Offer/OfferItem line economics + `PricingMethod` + item-type extension + `OfferCalculationService` line-economics pass — Backend Prompt

> **Module:** `Aizen.Modules.ServiceRequest`. **Phase:** ServiceRequest S1 (roadmap `docs/V1.0.1/ServiceRequest/ROADMAP.md`).
> **Canonical:** `docs/V1.0.1/COMMISSION_PACKAGE_PRICING.md` §20.3–20.5 (line pricing method + line economics),
> §20.11 (line commission eligibility dimension — the DIMENSION only; rate resolution is S7), §20.15 (line→aggregate; the
> full snapshot is S8). First of the narrow P8 core (S1 → S7 → S8 → Payment P8).
> **Rule:** EXTEND `ServiceRequestOfferEntity`/`ServiceRequestOfferItemEntity`/`OfferCalculationService`/enums; do NOT
> rewrite them and do NOT touch the acceptance flow (`AcceptServiceRequestOfferCommandHandler`) or Payment module.
> **Non-goal:** no commission RATE/amount (S7), no funding split (S6), no line snapshot rows (S8), no FX/TL fixing (S3),
> no attributes/travel/part-terms (S2/S4/S5). S1 lays the line-economics substrate the later phases fill. Inspect first.

## 0. Verified current state
- `ServiceRequestOfferItemEntity`: `ItemType, Title, Quantity, UnitPrice, CurrencyCode("USD"), TaxRate, DiscountType,
  DiscountValue` inputs → computed `LineSubtotal, DiscountAmount, TaxAmount, LineTotal` via `SetComputedTotals`.
- `ServiceRequestOfferEntity`: `Subtotal, DiscountTotal, TaxTotal, GrandTotal` + per-type totals (Service/Product/Labor/
  Installation/Inspection/Delivery/EmergencyFee/Other) via `SetComputedTotals`; `CurrencyCode` default "USD".
- `OfferCalculationService.Calculate(offer)` is **server-authoritative** (client numbers never trusted): line subtotal →
  discount lines (Amount/Percent) → pro-rata discount per priced line → per-line tax → grandTotal → per-type totals.
  `Round(x) = Math.Round(x, 2, AwayFromZero)`.
- `enum ServiceRequestOfferItemType { Service=1, Product=2, Installation=3, Delivery=4, Labor=5, Inspection=6,
  EmergencyFee=7, Discount=8, Other=99 }`; `OfferDiscountType/OfferDepositType { None=0, Amount=1, Percent=2 }`.
- Acceptance: `AcceptServiceRequestOfferCommandHandler` already creates payment escrow + publishes
  `ServiceRequestOfferAcceptedMessage` — **the P8/S10 hook; S1 does not touch it.**
- Payment BE-P2 already added `LineType` + `CommissionEligibility` **dimensions to `CommissionRuleEntity`** — S1's per-line
  `CommissionEligibility` must be expressed so S7 can feed it into the Payment resolver's dimension (align names/semantics).

## 1. `ServiceRequestOfferItemType` extension (§20.3)
Add expense/pass-through line types (int values, no collision): `Consumable=9, Travel=10, ExternalService=11,
EquipmentRental=12, MarinaOrLiftFee=13, OtherApprovedExpense=14`. Keep existing values. These join the priced-line set in
`OfferCalculationService` (they are NOT discount lines). Per-type aggregate exposure for the new types is **optional**
(the existing 8 per-type totals stay; new ones roll into `OtherTotal` unless a column is added — do NOT add columns in S1,
roll into Other, note it). Document the **economic role** of each type (revenue-bearing vs pass-through expense) for
default commission-eligibility (below) — but the actual role→eligibility mapping is admin/category-tunable, not hardcoded
business constants.

## 2. `PricingMethod` (§20.4) — per line
New `enum PricingMethod { Fixed=1, PerUnit=2, PerHour=3, PerDay=4, PerKm=5, PerLiter=6, PerSquareMeter=7,
EstimateRange=8, AfterInspection=9, TimeAndMaterials=10 }` (Abstraction/Enum). Add `PricingMethod` to
`ServiceRequestOfferItemEntity` (default `Fixed` for backfill). **S1 keeps the money math unchanged:** `LineSubtotal =
Round(Quantity × UnitPrice)` regardless of method — `PricingMethod` is **descriptive metadata** (how the price was
derived: Quantity=hours for PerHour, =km for PerKm, etc.) that S2/S4 (attributes/travel) and the FE offer builder will use.
`EstimateRange`/`AfterInspection` semantics (non-fixed offers) are S11 — S1 only stores the enum; do not branch the calc.

## 3. Per-line economics fields (§20.5) — substrate only
Add to `ServiceRequestOfferItemEntity` (computed by the calc service, private setters, extend `SetComputedTotals` or add a
second `SetComputedEconomics`):
- `CommissionEligibility` (`enum LineCommissionEligibility { Eligible=1, Exempt=2, InheritFromCategory=3 }`) — **input**,
  default derived from `ItemType` economic role (e.g. Service/Labor/Installation → Eligible; Travel/MarinaOrLiftFee/
  ExternalService/OtherApprovedExpense pass-through → default Exempt; Product/Consumable → InheritFromCategory), **but the
  default map is admin/category-tunable, not baked** — S1 provides a sane default + allows explicit override per line.
- `CommissionBaseAmount` (**computed**) — the amount to which commission WILL apply in S7. Definition (§20.5, document as
  the S1 decision): commission base = the line's **pre-tax, post-line-discount** revenue share, i.e.
  `Max(LineSubtotal − LineDiscountAmount, 0)` for `Eligible` lines, `0` for `Exempt` lines, and for `InheritFromCategory`
  the base is computed the same as Eligible but the *rate* decision defers to S7's category resolution. **No rate here.**
- Leave `ProviderNetAmount` / `PlatformContributionAmount` per line **out of S1** (they need commission rate S7 + funding
  S6); do NOT add stub columns that would be misleadingly zero — add them in S7/S8 when they can be correctly computed.

## 4. `OfferCalculationService` line-economics pass (§20.5)
Extend `Calculate(offer)` **after** the existing tax pass, without altering existing subtotal/discount/tax/grandTotal math:
- For each priced line compute `CommissionBaseAmount` per §3 (uses the already-computed `LineSubtotal` and the pro-rata
  `DiscountAmount`). Pass-through/`Exempt` lines → base 0.
- Aggregate `CommissionBaseTotal = Σ line.CommissionBaseAmount` on the offer (**new** offer field, computed) — this is the
  eligible base the transaction-level commission (S7/P8) will reconcile against.
- Keep everything **server-authoritative and pure** (no persistence side effects beyond entity mutation; no Payment calls;
  no snapshot). Deterministic; `Round` per existing convention. `CommissionBaseTotal ≤ Subtotal` invariant (guard/test).

## 5. Persistence + validation
- EF config: new columns `PricingMethod` (int), `CommissionEligibility` (int), `CommissionBaseAmount` (`numeric(18,4)`) on
  `service_request_offer_items`; `CommissionBaseTotal` (`numeric(18,4)`) on `service_request_offers`. `HasConversion<int>()`
  for enums. Append-only migration; backfill existing rows (`PricingMethod=Fixed`, `CommissionEligibility` by the default
  map, `CommissionBaseAmount`/`CommissionBaseTotal` recomputed via the calc service or a data step). No destructive change.
- Validation (FluentValidation on create/update offer item): `PricingMethod` defined; `CommissionEligibility` defined;
  Quantity ≥ 0; the new expense item types accepted. Do not weaken existing validators.
- Wherever `OfferCalculationService.Calculate` is already invoked (save/preview/submit), the new fields populate
  automatically — verify all call sites recompute (no path that persists an offer without the economics pass).

## 6. Tests
- **Enum extension:** new item types priced (not treated as discount); roll into Other aggregate; existing per-type totals
  unchanged.
- **PricingMethod:** stored per line; money math identical to Fixed for the same Quantity×UnitPrice (descriptive only).
- **CommissionBase:** Eligible line → base = LineSubtotal − proRataDiscount (pre-tax); Exempt (Travel/MarinaFee) → 0;
  `CommissionBaseTotal = Σ`; `CommissionBaseTotal ≤ Subtotal`; discount reduces base correctly (pro-rata).
- **Determinism/authoritative:** client-supplied economics ignored; recompute equals expected; rounding per convention.
- **Backfill:** migrated existing offer → CommissionBaseTotal recomputed, PricingMethod=Fixed, eligibility by default map.
- **No acceptance/Payment touch:** acceptance handler + Payment module unchanged (compile + existing tests green).

## 7. Acceptance criteria
- Item-type enum extended; `PricingMethod` stored (descriptive, math unchanged); per-line `CommissionEligibility` (tunable
  default by economic role) + computed `CommissionBaseAmount` + offer `CommissionBaseTotal`, all server-authoritative via
  the extended `OfferCalculationService`. Base = pre-tax post-discount for eligible lines, 0 for exempt.
- No commission rate/amount (S7), no funding (S6), no line snapshot (S8), no FX/TL (S3), no acceptance/Payment change.
  Migration append-only + backfill; build clean; existing offer calc/tests unaffected.

## 8. Verify — run and PASTE output
1. `dotnet build` ServiceRequest: 0 errors.
2. Rebuild + `docker compose up -d --force-recreate` the SR API; clean boot; migration + backfill applied.
3. DB: `SELECT "ItemType","PricingMethod","CommissionEligibility","CommissionBaseAmount" FROM service_request.service_request_offer_items LIMIT 10;`
   + `SELECT "Subtotal","CommissionBaseTotal" FROM service_request.service_request_offers LIMIT 5;` (base ≤ subtotal).
4. Tests green (paste): enum pricing, PricingMethod descriptive, commission-base eligible/exempt + total + ≤subtotal,
   determinism, backfill, acceptance/Payment untouched.
5. Smoke: build an offer with a Service line + a Travel line → Service line base = its net, Travel base = 0,
   CommissionBaseTotal = Service base.

## 9. Report
`REPORT_BACKEND.md` ("BE-S1"): item-type extension + PricingMethod + per-line CommissionEligibility/CommissionBaseAmount +
offer CommissionBaseTotal + OfferCalculationService line-economics pass + migration/backfill. Note: line commission
rate/amount = S7; line discount funding = S6; line snapshots→aggregate 8-invariant = S8; FX/TL fixing = S3;
attributes/travel/part-terms = S2/S4/S5; acceptance→economics→snapshot wiring = P8/S10. Next: **BE-S7 (line-level
commission eligibility/base resolved via Payment `CommissionRule` dimensions)**. Do not touch CargoDry.
