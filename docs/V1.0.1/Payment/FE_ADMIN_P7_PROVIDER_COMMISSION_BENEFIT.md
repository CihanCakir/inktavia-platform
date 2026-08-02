# FE_ADMIN_P7 — ProviderCommissionBenefitRule + Entitlement admin screens (VERTICAL SLICE — completes the rule-CRUD wave)

> **Fifth and final admin rule phase of the P1–P12 FE wave (after P2/P3/P5/P6).** Two things: **Part A —
> ProviderCommissionBenefitRule** (the most feature-rich rule yet: multi-category, `Stackable`/`Exclusive`,
> `AdjustmentPercentagePoints`, min-rate floor, GMV/usage caps → full `rule-crud` reuse incl. Priority + Specificity) and
> **Part B — ProviderCommissionBenefitEntitlement** (grant a benefit rule to a specific provider + revoke + list).
> Same vertical-slice template: add the missing list/detail/reactivate, then build the FE. Part A first (verify), then
> Part B.
>
> **Do NOT touch** provider-web, CargoDry, Identity, Keycloak, or the Commission/PlatformFee/ProfitProtection/
> CustomerDiscount surfaces. Enum fields stay **string**; rates fractions; percentage-points are pp. Conflict
> `ProviderCommissionBenefitConflict` = **5070** is already in `RULE_CONFLICT_CODES` + carried by
> `AdminPaymentBffFailEnvelopeHandler` → typed banner automatic.
>
> **Decoupling invariant (BE-P7):** these benefit rules only ever LOWER commission (never premium/boost). Keep that framing
> in the UI copy; do not imply they grant marketplace visibility.

## Ground truth (confirmed)
- **No admin ProviderCommissionBenefit rule/entitlement screen exists.** ⚠️ The existing `EntitlementMonitoringPage` is
  **all-mock and a DIFFERENT concept** (subscription-plan entitlement matrix: maxOffers/analytics/discount/inkCoin) —
  **do NOT reuse or conflate it.** Build a new ProviderCommissionBenefit surface.
- **Module `ProviderCommissionBenefitController`** (`api/v1/payment/commission-benefits`): `GET resolve`, `POST rules`,
  `PUT rules/{id}`, `POST rules/{id}/deactivate`, `POST entitlements` (grant), `POST entitlements/{id}/revoke`,
  `POST reserve`, `POST usages/{id}/consume|release` (runtime). **Missing: rule list/detail/reactivate; entitlement
  list/detail.**
- **Repos:** the rule + entitlement repos should have `GetById`/`GetAll` (verify; add `GetAllAsync` if the entitlement
  repo lacks it, as with P6 budget). No paging → `GetAllAsync`.
- **FE reusable blocks:** Part A uses the FULL rule-crud set (Priority + Specificity + conflict banner). Part B uses
  `useRuleMutation` + `RuleConflictBanner` for grant errors.
- **Seed note (BE-P7):** benefits are seeded **DISABLED** (an Inactive example −1pp / min 0.08). The list should show it
  as Inactive.

**BFF DTOs (`ProviderCommissionBenefitBffDtos.cs`) — exact fields:**
- `CreateProviderCommissionBenefitRuleBffRequest`: `RuleName?`, `ProviderProfileId?`, `ProviderPlanId?`,
  `ApplicableCategoryCodes: List<string>?` (multi-category), `AdjustmentPercentagePoints` (decimal, the −Npp), 
  `MinimumCommissionRate` (floor), `MaximumDiscountAmount?`, `MaximumEligibleGMV?`, `UsageLimit?` (long),
  `Stackable` (bool), `Exclusive` (bool — **must NOT be both true**), `Priority`, `EffectiveFrom`, `EffectiveTo?`,
  `CurrencyCode`, `Notes?`. Update = same minus structural targeting (ProviderProfileId/ProviderPlanId/Currency fixed).
  Result `(Id, RuleCode)`. Resolve → `EffectiveCommissionResolveBffResult` (BaseRuleCode, BaseRate,
  AppliedBenefitRuleCodes[], RequestedAdjustment, AppliedAdjustment, EffectiveCommissionRate, RequestedBenefitAmount,
  AppliedBenefitAmount, BenefitedServiceAmount, NonBenefitedServiceAmount, AdjustmentReason?).
- `GrantProviderCommissionBenefitEntitlementBffRequest`: `ProviderProfileId`, `BenefitRuleId`, `GrantedFrom`,
  `GrantedTo?`, `UsageLimit?`, `MaximumEligibleGMV?`. Results `GrantEntitlementBffResult(Id, EntitlementCode)`,
  `RevokeEntitlementBffResult(Id, EntitlementCode?)`.
- Domain (BE-P7): two-stage resolver ON TOP of the P2 base rate (`EffectiveRate = Max(Base + ΣAdjustment, MinRate)`);
  §19.5 controls (stackable sum / exclusive→conflict; MaximumDiscountAmount clamp; MaximumEligibleGMV benefited-split);
  entitlement usage ledger (UsedCount/ConsumedGMV/ReservedGMV). `ProviderCommissionBelowFloor` if a rule-min + system
  floor is violated.

## Part A — ProviderCommissionBenefitRule (full vertical slice)
1. **Module:** add `GET rules` (list via `GetAllAsync`; filter provider/plan/category/active/stackable), `GET rules/{id}`,
   `POST rules/{id}/reactivate` (re-check overlap → `ProviderCommissionBenefitConflict`). Query handlers + DTOs mirroring
   P6; no migration; unit tests (list/detail/reactivate-conflict).
2. **BFF:** List/Detail/Reactivate remote-calls + queries + typed `…ListItemBffDto`/`…ListBffResult`/`…DetailBffDto` + 3
   `AdminPaymentController` routes (AdminPanelAccess).
3. **FE:** `ProviderCommissionBenefitRulesPage` (+ detail); routes + nav; endpoints/paymentApi/`useProviderCommissionBenefitRulesQuery`.
   FULL rule-crud reuse (Priority + Specificity). **Form:** targeting (`ProviderProfileId?`, `ProviderPlanId?`,
   `ApplicableCategoryCodes` multi-select); `AdjustmentPercentagePoints` (labelled as a commission REDUCTION in pp);
   `MinimumCommissionRate` (floor); `MaximumDiscountAmount?`, `MaximumEligibleGMV?`, `UsageLimit?`; **`Stackable` /
   `Exclusive` toggles with a client-side guard: not both true**; `Priority`, `EffectiveFrom/To`, `CurrencyCode`,
   `RuleName?`, `Notes?`. Update omits structural targeting. **Resolve preview:** shows base rule/rate → applied benefit
   rules → effective rate + benefited/non-benefited GMV split (`EffectiveCommissionResolveBffResult`). i18n tr+en.

## Part B — ProviderCommissionBenefitEntitlement (grant / revoke / list)
1. **Module:** add `GET entitlements` (list; filter provider/rule/active) + `GET entitlements/{id}` (add
   `GetAllAsync`/`GetByIdAsync` to the entitlement repo if missing). Grant (`POST entitlements`) + revoke
   (`POST entitlements/{id}/revoke`) already exist. Reserve/consume/release stay untouched (runtime, not admin). Unit
   tests for the list.
2. **BFF:** List/Detail remote-calls + queries + typed DTOs + `GET commission-benefits/entitlements`,
   `GET .../entitlements/{id}` routes; Grant/Revoke command handlers + `POST .../entitlements`,
   `POST .../entitlements/{id}/revoke` routes (AdminPanelAccess).
2. **FE:** an entitlements section (own page or a tab on the rule detail): **list** granted entitlements (provider,
   benefit rule, granted window, UsageLimit/MaxGMV, used/consumed, status); **grant** action (pick ProviderProfileId +
   BenefitRuleId + GrantedFrom/To? + UsageLimit? + MaximumEligibleGMV?) via `useRuleMutation` (grant conflicts → banner);
   **revoke** with a confirm dialog. Show usage (UsedCount/ConsumedGMV) **read-only**. i18n tr+en. Do NOT build
   reserve/consume/release controls.

## Don't-break / QA
Stitch design; envelope-tolerant (real DTO names); reuse rule-crud blocks; `npm run typecheck` clean; module + BFF build
0; existing screens (incl. the mock EntitlementMonitoringPage — leave it) unregressed; provider-web/CargoDry git-clean.

## Verification (on-screen; keycloak-init ran, fresh login admin.user@inktavia.com, OTP from identity-api logs)
Part A: the seeded **Inactive** benefit rule lists; create a rule (multi-category, Stackable XOR Exclusive enforced,
AdjustmentPercentagePoints + MinimumCommissionRate); overlapping rule at same specificity → **typed 5070 conflict
banner**, save blocked; update; deactivate + reactivate; resolve preview shows base→effective rate + benefited split.
Part B: grant an entitlement to a provider for a rule → appears in the entitlements list; revoke it → status flips;
usage counters render read-only. typecheck/build/tests clean.

## Report
`docs/V1.0.1/Payment/REPORT_FE_ADMIN_P7.md`: rule + entitlement module/BFF endpoints, the Stackable/Exclusive guard, the
grant/revoke UX, on-screen transcript incl. the typed 5070 banner. **This completes the admin rule-CRUD wave
(P2/P3/P5/P6/P7).** Next options: provider Wave B (finance transparency), admin operational queues (P10 refund/
chargeback + I1 sub-merchant KYC), or P12 financial reporting dashboard.
