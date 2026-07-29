# BE-P8b — Wire P6 discount + S6 allocation + budget reserve/consume + P7 effective commission into P8 (close the seam) — Backend Prompt

> **Module:** `Aizen.Modules.Payment` (extend the P8 calc/handler) + the SR acceptance path. **Phase:** the second half of
> the P8 fast-follow (S6 ✅ → **P8-wiring**).
> **Canonical:** `docs/V1.0.1/COMMISSION_PACKAGE_PRICING.md` §19.9 (12-step, now with non-zero discount/benefit), §19.10
> (safe-max platform-funded discount → `ApprovedWithAdjustment`), §19.6–19.7 (funding + budget), §19.5 (P7 effective rate),
> §20.15 (8 equalities now with real discounts).
> **Rule:** EXTEND `ServiceRequestPaymentEconomicsCalculationService` + `CalculateServiceRequestEconomicsCommandHandler`;
> do NOT rewrite the resolvers (S7 `LineCommissionResolver`, P3 `PlatformFeeCalculationService`, P5 engine, S8
> `CreateFromLines`, P6 `CustomerDiscountBenefitService`/`CustomerBenefitBudgetService`, P7
> `ProviderCommissionBenefitResolver`/`CommissionBenefitEntitlementService`). Inspect first.

## 0. Verified current state (the exact seams to close)
- `ServiceRequestPaymentEconomicsCalculationService`:
  - line 75 `LineCommissionResolver.Resolve(...)` → per-line **base** commission (no P7 effective rate).
  - line 84 `customerPayable = Σ (LineGrossBeforeDiscount + LineVat)` (**VAT-inclusive, no P6 discount**).
  - builds the P5 `ProfitProtectionContext` + calls `CreateFromLines`.
- `CalculateServiceRequestEconomicsCommandHandler`: `discountAmount: 0m` (narrow core), `vatOnCommission: 0m` (pre-tax,
  YMM), feeGross from snapshot.
- `PaymentEconomicsSnapshotEntity` already has `TotalCustomerDiscountSnapshot`, `TotalProviderFundedDiscountSnapshot`,
  `TotalPlatformFundedDiscountSnapshot` + a `_discountAllocations` child collection (**populated empty today**).
- DI already present: `CustomerDiscountBenefitService`, `CustomerBenefitBudgetService`, `ProviderCommissionBenefitService`,
  `CommissionBenefitEntitlementService`. S6 gave `OfferCustomerDiscountAllocator` + `ResolveCustomerDiscountForOffer` +
  per-line `LineDiscountEligibility`.
- BE-I1 split-eligibility gate + BE-P9 pre-send split guard remain in force (unchanged).

## 1. Wire the customer discount (P6 + S6) into the calc (§19.9-2..4)
- Resolve the P6 `CustomerDiscountRule` for (customerPlan, category, currency) via the existing resolver/service (the
  authoritative call, not the S6 preview) → requested discount + funding mode + consent.
- Apply **S6's `OfferCustomerDiscountAllocator`** to split it across eligible lines (pre-tax base), then apply S6's
  **pre-tax + tax-recompute** per line: reduce each eligible line's taxable base, VAT, `LineTotal`, and `CommissionBase`.
- Aggregate `TotalCustomerDiscount`, `TotalPlatformFundedDiscount`, `TotalProviderFundedDiscount`, `TotalSupplierFunded(0)`.
- **Platform-fee-base VAT decision (resolve the P8 flag):** `CustomerPayableServiceAmount = Σ (post-discount LineTotal)`
  (VAT-inclusive, so S8's `Σ lineTotal + PlatformFeeGross = CustomerTotal` holds); `PlatformFeeBaseAmount =
  CustomerPayableServiceAmount`. **Document this as the decision** (platform fee on the VAT-inclusive post-discount
  payable), flag as YMM-revisitable, and record the basis in the snapshot. Do not silently mix pre-tax and VAT-inclusive.

## 2. Wire P7 effective commission (§19.5, §19.9-8/9)
- After S7 base per-line commission, apply `ProviderCommissionBenefitResolver.ResolveEffectiveCommission` per eligible line
  → `EffectiveCommissionRate` (floored; benefits OFF by default so this is a no-op until an entitlement is active) →
  recompute per-line `CommissionAmount = Round(CommissionBase × EffectiveRate)` and `ProviderNet`. `ProviderCommissionBenefitCost
  = Σ (base − effective) commission`. Feed the entitlement into `CommissionBenefitEntitlementService` reserve at §4.

## 3. Reserve / consume / release ordering (§19.7, §8) — acceptance lifecycle
- **Reserve** (in the calc/create path, BEFORE checkout): `CustomerBenefitBudgetService.Reserve(platformFundedDiscount,
  contextRef=SR-{sr}-OFFER-{offer})` for the platform-funded customer benefit; `CommissionBenefitEntitlementService`
  reserve for any active P7 benefit (usage/GMV). Order: resolve economics → reserve budget/entitlement → P5 gate →
  snapshot → escrow.
- **Consume** on successful escrow/payment; **Release** on failure/reject/timeout. Idempotent (unique contextRef — a retry
  or duplicate webhook does not double-reserve/consume; reuse the BE-P6/P7 concurrency guards).
- If reserve fails (budget insufficient) → the platform-funded discount is capped to remaining (feeds §19.10 below) or the
  decision reflects it — never overspend the budget.

## 4. P5 with real inputs + safe-max adjustment (§19.10, §19.11) — now live
- Build the `ProfitProtectionContext` with the **real** `RequestedPlatformFundedCustomerDiscount`,
  `ProviderFundedCustomerDiscount`, `CustomerBenefitBudgetRemaining`, `ProviderCommissionBenefitCost` (no longer 0).
- Run the P5 engine. If `ApprovedWithAdjustment` (platform-funded discount reduced to safe-max §19.10) → **recompute the
  economics with the adjusted discount** (second pass: re-allocate the reduced discount via S6, re-apply pre-tax/tax,
  re-run commission) so the snapshot reflects the final adjusted amounts (§19.11 final-before-checkout, no silent change).
  If `Rejected`/`ConfigurationError` → no snapshot/escrow, **release** any reservation.

## 5. Populate `DiscountAllocationSnapshot` (S8) + re-verify the 8 equalities with real discounts
- Feed the per-line `{FundingSource, DiscountAmount}` into `CreateFromLines` so `_discountAllocations` rows are written
  (Platform/Provider/Shared per line). The now-non-zero equalities MUST hold with **zero tolerance**:
  `Σ line customer discount = TotalCustomerDiscount`, `Σ provider-funded = TotalProviderFundedDiscount`,
  `Σ platform-funded = TotalPlatformFundedDiscount`, and the existing five. `PaymentEconomicsInvariantException` on any
  mismatch.

## 6. Tests
- **Discount end-to-end:** GOLD 5% PlatformFunded on {Service 5000 eligible, Travel 800 exempt} → Service discount 250
  pre-tax, tax recomputed, `CommissionBase` 4750, commission 570 (STANDARD 0.12), providerNet 4930+800; `CustomerPayable =
  Σ post-discount LineTotal`; platform fee on that; snapshot `TotalCustomerDiscount=250`, `TotalPlatformFundedDiscount=250`;
  DiscountAllocation rows present; 8 equalities hold.
- **Funding modes:** ProviderFunded (consent yes/no), Shared → per-line + aggregate funded totals correct; provider-funded
  discount does NOT reduce platform revenue incorrectly.
- **P7 effective rate:** with an active −1pp entitlement → effective 0.11, commission recomputed, `ProviderCommissionBenefitCost`
  set, entitlement reserved/consumed; benefits-off → identical to base (no-op).
- **Budget lifecycle:** reserve decrements remaining; consume on success; **release on reject**; idempotent duplicate;
  insufficient budget → discount capped, no overspend.
- **P5 adjustment:** an unsafe platform-funded discount → `ApprovedWithAdjustment`, discount reduced to safe-max,
  **economics recomputed**, snapshot = adjusted amounts, gates pass; reject → no snapshot + reservation released.
- **8 equalities with real discounts:** all hold zero-tolerance; tamper → exception.
- **Idempotency:** SR-{sr}-OFFER-{offer} retry → one snapshot, one reserve/consume, MarkApplied once.
- **Narrow-core regression:** no customer discount + benefits off → byte-identical to BE-P8 (discount 0 path unchanged).

## 7. Acceptance criteria
- P6 customer discount + S6 line allocation applied pre-tax (base/VAT/CommissionBase reduced); `CustomerPayable` =
  Σ post-discount LineTotal, platform-fee base = that (**documented VAT decision**, YMM-revisitable, snapshotted); P7
  effective commission per line; budget/entitlement reserve→consume/release idempotent at acceptance; P5 with real inputs +
  safe-max `ApprovedWithAdjustment` recompute; `DiscountAllocationSnapshot` populated; **all 8 §20.15 equalities hold
  zero-tolerance with non-zero discounts**; reject releases reservations, no snapshot.
- Existing resolvers/gates (S7/P3/P5/P7/I1/P9-guard) untouched-except-wired; narrow-core path byte-identical; build clean.
  No CargoDry.

## 8. Verify — run and PASTE output
1. `dotnet build` Payment + ServiceRequest: 0 errors.
2. Rebuild + recreate APIs; migration (if any) append-only.
3. Tests green (paste): discount end-to-end + 8 equalities, funding modes+consent, P7 effective rate, budget reserve/
   consume/release + idempotent + insufficient-cap, P5 ApprovedWithAdjustment recompute + reject-release, narrow-core
   regression byte-identical.
4. Smoke: accept an offer with a GOLD 5% platform-funded discount → snapshot shows discount + funding + DiscountAllocation
   rows; escrow gross = CustomerTotal (post-discount) and split = ProviderNet; an unsafe discount → adjusted-and-recomputed.

## 9. Report
`REPORT_BACKEND.md` ("BE-P8b"): wired P6 discount + S6 allocation (pre-tax/tax-recompute) + platform-fee-base VAT decision +
P7 effective commission + budget/entitlement reserve/consume/release + P5 real-input safe-max adjustment recompute +
DiscountAllocationSnapshot + non-zero 8-equality re-verify. Note: the P8 VAT flag is now resolved (documented); live P9
sandbox gate still awaits keys. Next: **BE-P10 (refund allocation + chargeback/clawback)** — refund must restore budget
(§19.15) + reverse commission/discount per the snapshot. Do not touch CargoDry.
