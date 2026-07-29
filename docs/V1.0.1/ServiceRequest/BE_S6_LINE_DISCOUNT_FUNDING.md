# BE-S6 — Line-level customer-discount eligibility + deterministic funding allocation (pre-tax → tax recompute) — Backend Prompt

> **Modules:** `Aizen.Modules.ServiceRequest` (line model + calc) + `Aizen.Modules.Payment` (P6 resolver reused via the S7
> remote-call pattern). **Phase:** ServiceRequest S6 — the first half of the P8 fast-follow (S6 → P8-wiring).
> **Canonical:** `docs/V1.0.1/COMMISSION_PACKAGE_PRICING.md` §20.10 (line discount default behaviour + deterministic
> order-level allocation), §19.6 (CustomerDiscountRule + funding modes). Feeds S8 `DiscountAllocationSnapshot` (populated at
> P8) and the P5 engine's discount inputs (wired at P8).
> **Rule:** EXTEND S1's `OfferCalculationService` line-economics + the offer item; do NOT rewrite. Reuse the S7 preview/
> remote-call pattern for resolving the P6 `CustomerDiscountRule`. Inspect first.
> **Non-goal:** no budget reserve/consume (that's P8, `CustomerBenefitBudgetService`), no snapshot persistence (S8/P8), no
> P7 benefit, no platform-fee-base change. S6 = eligibility + the **deterministic allocation mechanics** + a preview.

## 0. Verified current state
- S1 gives per line: `CommissionEligibility`, `CommissionBaseAmount`; `OfferCalculationService.Calculate` already applies
  the provider's own **offer discount lines** pre-tax (pro-rata) then recomputes tax → `LineTotal` (VAT-inclusive). This is
  the mechanic S6 extends for the **platform/plan customer discount** (a different thing from the provider's offer
  discount lines).
- P6 ready: `CustomerDiscountRuleResolver` (specificity + fail-loud), funding modes Platform/Provider/Shared(/Supplier),
  `CustomerBenefitBudgetService.Reserve/Consume/Release` (reserve/consume = **P8**). S7 established the cross-module preview
  pattern (SR → `IPaymentModuleRemoteCall` internal typed endpoint → Payment resolver, compute-on-demand, no persistence).
- S8 `DiscountAllocationSnapshotEntity` exists (FundingSource + amount per line) — **rows written at P8**, S6 only produces
  the numbers.

## 1. Distinguish the two discounts (§20.10)
- **Provider offer discount lines** (existing S1 `ItemType=Discount`) — the provider's own price reduction, already handled.
- **Customer discount** (P6 `CustomerDiscountRule`) — a platform/plan/campaign discount (e.g. GOLD member 5%), with a
  **funding source** (who pays for it). S6 introduces this as a distinct, funded, line-allocated reduction. **No anonymous
  single offer-wide discount** (§20.10) — it is always allocated to eligible lines.

## 2. Line discount eligibility
Add per line `LineDiscountEligibility { Eligible=1, Exempt=2, InheritFromCategory=3 }` (mirror `CommissionEligibility`),
default by ItemType economic role (Service/Labor/Product → Eligible; Travel/MarinaFee/pass-through → Exempt), admin-tunable
+ per-line override. **Exempt lines receive no customer discount** (§20.10). Validation like S1.

## 3. Deterministic allocation mechanics (pure) — the heart
`OfferCustomerDiscountAllocator` (SR Application/Services or Payment.Domain if it must sit with the resolver — keep the
allocation in SR since it operates on offer lines; the RULE resolution stays in Payment):
`Allocate(eligibleLines, requestedDiscountAmount, fundingMode, platformRate?/providerRate?) → per-line
{ CustomerDiscountAmount, PlatformFundedAmount, ProviderFundedAmount, SupplierFundedAmount(0) }`:
- **Base for allocation** = each eligible line's **pre-tax post-provider-discount** amount (the S1 `CommissionBase`-style
  net, i.e. `Max(LineSubtotal − proRataProviderDiscount, 0)` for eligible lines; 0 for exempt).
- **Deterministic pro-rata:** `lineDiscount = Round(requestedDiscount × lineBase / Σ eligibleBase)`; fix the rounding
  remainder on the largest-base line so `Σ lineDiscount == requestedDiscount` exactly (deterministic, order-stable).
- **Funding split per line** (§19.6): PlatformFunded → all to platform; ProviderFunded → all to provider (consent flag
  from P6 — without consent the provider portion is dropped, not platform-shifted); Shared → by platform/provider rates
  (sum 100%); Supplier = 0. Σ over lines per source is exposed.
- Clamp: total customer discount cannot exceed Σ eligibleBase.

## 4. Pre-tax application + tax recompute (VAT treatment for the discount — documented)
The customer discount is applied **pre-tax, per line** (consistent with S1's existing provider-discount→tax mechanic and TR
practice): for each eligible line, `discountedBase = lineBase − lineCustomerDiscount` → **recompute line tax** on
`discountedBase` → new `LineTotal = discountedBase + recomputedTax`. So a customer discount reduces both the taxable base
and the VAT. `CommissionBaseAmount` (S1) is also reduced to the post-customer-discount pre-tax base (commission follows the
discounted service value). **Document this as the S6 decision.** (The separate question of whether the *platform-fee base*
is pre-tax or VAT-inclusive is a P8-wiring/YMM decision — NOT decided here; S6 only fixes the discount-application basis.)

## 5. `OfferCalculationService` extension + preview
- Extend `Calculate` with an optional customer-discount input (per-line allocated amounts from §3): when present, apply §4
  (pre-tax, tax recompute, reduce CommissionBase), and expose per-line `CustomerDiscountAmount` +
  `{Platform/Provider/Supplier}FundedAmount` + offer totals `TotalCustomerDiscount`, `TotalPlatformFundedDiscount`,
  `TotalProviderFundedDiscount`. When absent (narrow core), behaviour is exactly today's (discount 0).
- **Preview** (mirror S7): a SR query `GetOfferCustomerDiscountPreview(offerId)` that resolves the P6 `CustomerDiscountRule`
  via `IPaymentModuleRemoteCall` (new typed internal method `ResolveCustomerDiscount`, `[Authorize]` service-to-service) for
  the offer's customer/category/currency, runs §3 allocation + §4 application on a COPY, and returns the per-line + funding
  breakdown **for display** (offer builder transparency). **No persistence, no budget reserve, no snapshot** (all P8).

## 6. Persistence / migration
- SR: add `LineDiscountEligibility` (int) to `service_request_offer_items` + `CustomerDiscountAmount`,
  `PlatformFundedDiscountAmount`, `ProviderFundedDiscountAmount` per line (nullable/0, computed) and
  `TotalCustomerDiscount`/`TotalPlatformFundedDiscount`/`TotalProviderFundedDiscount` on the offer (computed). Append-only
  migration + backfill (eligibility by default map, amounts 0). These persist the offer-builder's chosen/previewed customer
  discount so P8 can reconcile — mark them **non-authoritative until P8 snapshots** (like S7's stance; if you prefer S6 to
  stay compute-on-demand with NO new SR columns, do that instead and note it — decide by whether the offer needs to
  remember a previewed discount).
- Payment: the `ResolveCustomerDiscount` remote-call method + typed DTOs; no new tables (P6 resolver is pure).

## 7. Tests
- **Eligibility:** exempt line gets 0 customer discount; eligible lines share it.
- **Deterministic allocation:** pro-rata by eligible base; rounding remainder fixed on largest line so `Σ == requested`
  exactly; order-stable; total clamped to Σ eligibleBase.
- **Funding:** Platform → all platform; Provider (consent yes/no — no consent → provider portion dropped, not
  platform-shifted); Shared → split by rates (sum 100%); Σ per source correct.
- **Pre-tax + tax recompute:** discount reduces taxable base AND VAT; `LineTotal` recomputed; `CommissionBase` reduced;
  a mixed offer (eligible Service + exempt Travel) → discount only on Service, Travel untouched.
- **Preview:** resolves P6 rule via remote-call, returns per-line + funding for display; typed round-trip + `[Authorize]`
  401; no persistence/budget/snapshot.
- **Narrow-core unchanged:** with no customer discount, all outputs identical to pre-S6.

## 8. Acceptance criteria
- Per-line customer-discount eligibility + deterministic funded allocation (Σ == requested, exempt excluded, funding split
  correct, provider-consent honoured); discount applied **pre-tax with tax recompute** and `CommissionBase` reduced
  (documented VAT basis for the discount); `OfferCalculationService` extended + a P6-resolving preview (no persistence/
  budget/snapshot). Platform-fee-base VAT question left to P8-wiring. Build clean; narrow-core behaviour unchanged; existing
  S1 tests green.

## 9. Verify — run and PASTE output
1. `dotnet build` ServiceRequest + Payment: 0 errors.
2. Rebuild + recreate both APIs; migration (if any) append-only applied.
3. Tests green (paste): eligibility, deterministic allocation Σ==requested + remainder, funding modes + consent, pre-tax
   tax-recompute + CommissionBase reduction, preview round-trip + 401, narrow-core unchanged.
4. Smoke: offer {Service 5000 eligible, Travel 800 exempt}, GOLD 5% PlatformFunded → Service discount 250 (pre-tax), tax
   recomputed on 4750, Travel untouched; `TotalCustomerDiscount=250`, `TotalPlatformFundedDiscount=250`, CommissionBase
   reduced to 4750.

## 10. Report
`REPORT_BACKEND.md` ("BE-S6"): line discount eligibility + deterministic funded allocation + pre-tax/tax-recompute +
CommissionBase reduction + P6-resolving preview (remote-call) + migration. Note: budget reserve/consume + P7 benefit
effective rate + DiscountAllocationSnapshot population + platform-fee-base VAT decision + non-zero 8-equality re-verify =
**P8-wiring (next)**. Do not touch CargoDry.
