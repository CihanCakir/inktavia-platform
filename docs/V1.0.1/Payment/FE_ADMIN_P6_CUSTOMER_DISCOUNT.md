# FE_ADMIN_P6 — CustomerDiscountRule + CustomerBenefitBudgetPolicy admin screens (VERTICAL SLICE)

> **Fourth admin phase of the P1–P12 FE wave.** P6 has **two entities**: **Part A — CustomerDiscountRule** (a
> specificity+funding rule, the closest yet to Commission/PlatformFee → full `rule-crud` reuse incl. Priority +
> Specificity) and **Part B — CustomerBenefitBudgetPolicy** (a per-plan budget policy, lighter). Same vertical-slice
> template as P3/P5: add the missing list/detail/reactivate, then build the FE. Do Part A first (verify), then Part B.
>
> **Do NOT touch** provider-web, CargoDry, Identity, Keycloak, or the Commission/PlatformFee/ProfitProtection surfaces.
> Enum fields stay **string** on the wire; rates are fractions. Conflicts (`CustomerDiscountRuleConflict` = **5060**,
> `CustomerBenefitBudgetPolicyConflict` = **5068**) are already in `RULE_CONFLICT_CODES` → the typed banner lights up
> automatically once the failure propagates (the `AdminPaymentBffFailEnvelopeHandler` already carries them).

## Ground truth (confirmed)
- **No admin CustomerDiscount / BenefitBudget screens exist** → build new.
- **CustomerDiscountRule** — module `CustomerDiscountRuleController` (`api/v1/payment/customer-discounts`): only
  `GET resolve`, `POST rules`, `PUT rules/{id}`, `POST rules/{id}/deactivate`. **Missing list/detail/reactivate.** Repo
  (`ICustomerDiscountRuleRepository`) has `ResolveAsync`, `FindOverlappingActiveRuleAsync`, `GetByIdAsync`,
  `GetAllAsync` (+ Add/Update). No paging → list via `GetAllAsync`.
- **CustomerBenefitBudgetPolicy** — module `CustomerBenefitBudgetController` (`api/v1/payment/benefit-budget`):
  `POST policies` (create), `POST reserve`, `POST reservations/{id}/consume`, `POST reservations/{id}/release`.
  **No list/detail/update/deactivate.** Repo (`ICustomerBenefitBudgetPolicyRepository`) has `ResolveAsync`,
  `FindOverlappingActivePolicyAsync`, `GetByIdAsync`, `Add`, `Update` — **but NO `GetAllAsync`** (add one for the list).
  Reserve/consume/release are **runtime** ops, NOT admin CRUD → out of scope (show usage read-only, don't build controls).
- **FE reusable blocks (`src/features/payments/rule-crud/`):** Part A uses the FULL set incl. `PriorityField` +
  `SpecificityHint` (it's a specificity rule); Part B uses `RuleConflictBanner` + `EffectiveDateRangeField` +
  `useRuleMutation` but NOT Priority/Specificity (per-plan policy).

**BFF DTOs (`CustomerDiscountBffDtos.cs`) — exact fields:**
- `CreateCustomerDiscountRuleBffRequest`: `CustomerPlanId?`, `CategoryCode?`, `CurrencyCode`, `DiscountType`
  (Percent|Fixed), `DiscountRate?`, `FixedDiscountAmount?`, `MinimumPurchaseAmount?`, `MaximumDiscountAmount?`,
  `FundingMode` (PlatformFunded|ProviderFunded|Shared|SupplierFunded), `PlatformFundingRate?`, `ProviderFundingRate?`,
  `RequiresProviderConsent?`, `Priority`, `EffectiveFrom`, `EffectiveTo?`, `RuleName?`, `Notes?`. Update = same minus the
  structural targeting (CustomerPlanId/CategoryCode/Currency fixed at create). Result `(Id, RuleCode)`.
  Resolve result `CustomerDiscountResolveBffResult`: RequestedDiscountAmount, RequestedPlatformFundedCustomerDiscount,
  ProviderFundedCustomerDiscount, AppliedDiscountAmount, UnappliedDueToConsent, CustomerBenefitBudgetRemaining,
  CustomerPlanRevenueAllocation.
- `CreateCustomerBenefitBudgetPolicyBffRequest`: `CustomerPlanId`, `CurrencyCode`, `BenefitBudgetRate`, `PerPeriodMax?`,
  `PerCategoryLimit?`, `PerTransactionLimit?`, `RefundRestorePolicy` (Restore|Consume), `EffectiveFrom`, `EffectiveTo?`,
  `Notes?`. Result `(Id, PolicyCode)`.
- Domain (BE-P6): discount specificity `CustomerPlan+Category > CustomerPlan > Category > Global`; funding
  Platform/Provider[consent absent → provider share drops, never silently to platform]/Shared/Supplier; budget
  reserve→consume/release with per-plan rate/limits + refund-restore.

## Part A — CustomerDiscountRule (full vertical slice, mirror P3 + Commission)
1. **Module:** add `GET rules` (list via `GetAllAsync`; filter customerPlanId/category/currency/funding/active), `GET
   rules/{id}`, `POST rules/{id}/reactivate` (re-check `FindOverlappingActiveRuleAsync` → `CustomerDiscountRuleConflict`).
   Query handlers + DTOs mirroring P3; no migration; unit tests (list/detail/reactivate-conflict).
2. **BFF:** List/Detail/Reactivate remote-calls + query handlers + typed `CustomerDiscountRuleListItemBffDto`/`…ListBffResult`/
   `…DetailBffDto` + 3 `AdminPaymentController` routes under AdminPanelAccess.
3. **FE:** `CustomerDiscountRulesPage` (+ detail); routes + nav; endpoints + paymentApi + `useCustomerDiscountRulesQuery`.
   Reuse the FULL rule-crud set (Priority, Specificity, conflict banner). **Form:** targeting (`CustomerPlanId?`,
   `CategoryCode?`, `CurrencyCode`); `DiscountType` select → Percent(`DiscountRate`) | Fixed(`FixedDiscountAmount`);
   `MinimumPurchaseAmount?`, `MaximumDiscountAmount?`; **`FundingMode` select** (Platform/Provider/Shared/Supplier) that
   conditionally shows `PlatformFundingRate`/`ProviderFundingRate` (Shared) + `RequiresProviderConsent` toggle (for
   ProviderFunded/Shared — with a note: "without consent, the provider share is dropped, not shifted to platform");
   `Priority`, `EffectiveFrom/To`, `RuleName?`, `Notes?`. Update omits the structural targeting. **Resolve preview:**
   pick plan/category/currency → shows requested vs applied discount, platform/provider funded split, unapplied-due-to-
   consent, and budget remaining (`CustomerDiscountResolveBffResult`). i18n tr+en.

## Part B — CustomerBenefitBudgetPolicy (lighter per-plan screen)
1. **Module:** add `GetAllAsync` to `ICustomerBenefitBudgetPolicyRepository` + impl; add `GET policies` (list),
   `GET policies/{id}`. (Update/deactivate/reactivate are NOT in the module today — **do not add them**; MVP is
   create + list + view. Note the gap in the report.) Reserve/consume/release stay untouched (runtime).
2. **BFF:** List/Detail remote-calls + query handlers + typed DTOs + `GET payment/benefit-budget/policies`,
   `GET .../policies/{id}` routes (create already exists via `CreateCustomerBenefitBudgetPolicy`).
3. **FE:** `CustomerBenefitBudgetPage` — per-plan budget policies list + a create form (`CustomerPlanId`,
   `CurrencyCode`, `BenefitBudgetRate`, `PerPeriodMax?`, `PerCategoryLimit?`, `PerTransactionLimit?`,
   `RefundRestorePolicy` Restore|Consume, `EffectiveFrom/To`, `Notes?`) using `RuleConflictBanner` (5068) +
   `EffectiveDateRangeField` + `useRuleMutation` (NO Priority/Specificity). Show budget **usage/remaining read-only**
   from resolve where available. i18n tr+en. (Editing an existing budget policy / deactivation is deferred — surface a
   note, don't fake controls.)

## Don't-break / QA
Stitch design; envelope-tolerant (real DTO names); reuse rule-crud blocks (Part A full, Part B minus Priority/
Specificity); `npm run typecheck` clean; module + BFF build 0; existing screens unregressed; provider-web/CargoDry
git-clean. Seed note: BE-P6 seeds GOLD 0.05 / PLATINUM 0.10 PlatformFunded discount rules (+ budget policies) — the
lists should show them.

## Verification (on-screen; keycloak-init ran, fresh login admin.user@inktavia.com, OTP from identity-api logs)
Part A: seeded discount rules list; create Percent + Fixed rules with each FundingMode (Shared shows funding rates +
consent); overlapping rule at same specificity → **typed 5060 conflict banner**, save blocked; update; deactivate +
reactivate; resolve preview shows the funding split + budget remaining. Part B: seeded budget policies list; create a
per-plan policy; overlapping per-plan/currency → **typed 5068 conflict banner**; usage/remaining renders read-only.
typecheck/build/tests clean.

## Report
`docs/V1.0.1/Payment/REPORT_FE_ADMIN_P6.md`: both entities' module/BFF endpoints, the funding-mode + consent UX, which
rule-crud blocks were reused per part, the deferred BenefitBudget edit/deactivate gap, on-screen transcript incl. both
typed conflict banners (5060/5068). Next in the wave: P7 ProviderCommissionBenefit rule.
