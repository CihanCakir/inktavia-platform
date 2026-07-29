# BE-P5 — `ProfitProtectionPolicy` + Profit Protection Engine (3 contribution gates, safe-max, decision states) — Backend Prompt

> **Module:** `Aizen.Modules.Payment`. **Phase:** Payment P5 (roadmap `docs/V1.0.1/Payment/ROADMAP.md`). **The heart of the
> revision — implement with rigor.**
> **Canonical:** `docs/V1.0.1/COMMISSION_PACKAGE_PRICING.md` §19.1–19.3 (three contributions + min policy), §19.10
> (safe-max platform-funded discount), §19.11 (decision states), §19.14 (models).
> **Rule:** New, self-contained engine; do NOT rewrite existing infra. Reuse BE-P2/P3/P4 versioned/effective-date +
> `MoneyMath` + fail-loud patterns and the `Domain/Entities/<X>/<X>Resolver.cs` convention.

## 0. Verified current state
- **No ProfitProtection infra → create.** Domain resolvers live at `Domain/Entities/<X>/<X>Resolver.cs` (pure);
  calculation services at `Application/Services/*CalculationService.cs`. Error codes in `PaymentErrorCode` (add 5050+).
  `MoneyMath` (BE-P1), fail-loud conflict + effective-date single-active patterns (BE-P2/P3/P4) reused.

## 1. Scope of P5 (and non-scope)
**In:** `ProfitProtectionPolicy` (versioned, effective-date, single-active) + `ProfitProtectionEngine` (pure, context-in →
decision-out) computing the **three contributions**, `Required = Max(minAmount, base×minRate)`, **expected variable
expenses** (from policy), the **safe-max platform-funded discount**, and the **decision** (`Approved /
ApprovedWithAdjustment / Rejected / ConfigurationError`); `ProfitProtectionEvaluationLog`; admin CRUD; tests.
**Out (later, engine consumes as INPUT):** `CustomerDiscountRule` + funding + benefit budget (P6),
`ProviderCommissionBenefitRule` (P7), and **wiring into acceptance** (`CalculateServiceRequestPaymentEconomics`, P8) +
snapshot write (P8). The engine is **pure and context-driven**; at P5 those inputs arrive via the context (0 in tests).

## 2. `ProfitProtectionPolicy` entity (versioned)
`Payment.Domain/Entities/ProfitProtection/ProfitProtectionPolicyEntity.cs : AizenEntityWithAudit`:
- Minimums (both amount AND rate per §19.3): `MinCustomerSideContributionAmount/Rate`,
  `MinProviderSideContributionAmount/Rate`, `MinTransactionContributionAmount/Rate`.
- Expected-expense policy: `PaymentProcessingExpenseRate` (on CustomerTotal) + optional `PaymentProcessingFixed`,
  `RefundRiskReserveRate` (on base), `OtherVariableExpenseRate/Fixed`.
- `AdjustmentOrder` (enum/ordered list — which advantage to reduce first when unsafe; e.g. PlatformDiscount →
  CommissionBenefit).
- `CurrencyCode`, `EffectiveFrom`, `EffectiveTo?`, `Status` (Active/Scheduled/Expired/Inactive), `PolicyCode` (unique).
- **Resolution** `ResolveActivePolicyAsync(currency, atUtc)` → exactly one active; >1 overlapping → fail-loud
  `ProfitProtectionPolicyConflict`; 0 → `ProfitProtectionPolicyNotFound` (→ ConfigurationError at engine).
- All values **admin-configurable, versioned**; NO hardcoded rate/amount constants (spec §19.3).

## 3. `ProfitProtectionContext` (engine input — value object)
Carries the computed economics + requested advantages + revenue allocations (assembled by P8 later; synthetic in tests):
`CurrencyCode, ServiceAmount, CustomerPayableServiceAmount, CustomerTotalAmount, ProviderNetAmount,
ProviderCommissionNetRevenue, CustomerPlatformFeeNetRevenue, CustomerPlanRevenueAllocation,
CustomerPremiumRevenueAllocation, ProviderPlanRevenueAllocation, ProviderAddOnRevenueAllocation,
ProviderCommissionBenefitCost, RequestedPlatformFundedCustomerDiscount, ProviderFundedCustomerDiscount,
CustomerBenefitBudgetRemaining` (from P6; 0 if none). Expected expenses are **derived by the engine from policy**, not
passed in.

## 4. Three contributions (§19.2) + Required (§19.3)
Engine computes (via `MoneyMath`):
```
ExpectedPaymentProcessingExpense = Round(CustomerTotalAmount × policy.PaymentProcessingExpenseRate) + policy.PaymentProcessingFixed
ExpectedRefundRiskReserve        = Round(CustomerTotalAmount × policy.RefundRiskReserveRate)
ExpectedOtherVariableExpenses    = Round(CustomerTotalAmount × policy.OtherVariableExpenseRate) + policy.OtherVariableExpenseFixed

CustomerSideContributionExpected = CustomerPlatformFeeNetRevenue + CustomerPlanRevenueAllocation
                                 + CustomerPremiumRevenueAllocation − PlatformFundedCustomerDiscount
                                 − CustomerSideVariableCostAllocation
ProviderSideContributionExpected = ProviderCommissionNetRevenue + ProviderPlanRevenueAllocation
                                 + ProviderAddOnRevenueAllocation − ProviderCommissionBenefitCost
                                 − ProviderSideVariableCostAllocation
TotalTransactionContributionExpected = ProviderCommissionNetRevenue + CustomerPlatformFeeNetRevenue
                                 + ApprovedSubscriptionRevenueAllocations + ApprovedAddOnRevenueAllocations
                                 − PlatformFundedCustomerDiscount − ExpectedPaymentProcessingExpense
                                 − ExpectedRefundRiskReserve − ExpectedOtherVariableExpenses

Required<X>Contribution = Max(policy.Min<X>ContributionAmount, <X>Base × policy.Min<X>ContributionRate)
```
> Variable-cost **allocation to customer/provider side** (`CustomerSideVariableCostAllocation`/`ProviderSide…`) and the
> **contribution base** for each side's rate (`<X>Base`) are policy-configurable; document defaults and mark exact
> split/base as an **open decision** (§19 open items). Sensible defaults: TransactionBase = CustomerTotalAmount,
> ProviderSideBase = ServiceAmount, CustomerSideBase = CustomerTotalAmount.

## 5. Gates (§19.2) + safe-max discount (§19.10)
Three binding gates (zero tolerance, exact decimal):
```
CustomerSideContributionExpected     ≥ RequiredCustomerSideContribution
ProviderSideContributionExpected     ≥ RequiredProviderSideContribution
TotalTransactionContributionExpected ≥ RequiredTransactionContribution
```
Safe-max platform-funded discount:
```
MaximumSafePlatformFundedDiscount = Max(0, PreDiscountExpectedContribution − RequiredTransactionContribution − ExpectedVariableExpenses)
  clamped to ≤ CustomerBenefitBudgetRemaining
  and must keep CustomerTotalAmount ≥ ProviderNetAmount
```
(`PreDiscountExpectedContribution` = the total contribution computed with PlatformFundedDiscount = 0.)

## 6. Decision (§19.11) — `ProfitProtectionDecision`
States: **`Approved`** (all requested advantages safe — all gates pass) · **`ApprovedWithAdjustment`** (requested advantage
unsafe → reduce per `AdjustmentOrder` to the safe maximum, re-evaluate; return applied amounts + `AdjustmentReason`) ·
**`Rejected`** (no safe combination meets the minimums) · **`ConfigurationError`** (missing/conflicting policy or
uncomputable funding). Result object:
`{ State, AppliedPlatformFundedDiscount, AppliedCommissionBenefit, AdjustmentReason?, Customer/Provider/TotalContributionExpected,
   Required{Customer,Provider,Transaction}, Expected{Processing,RefundReserve,Other}Expense, MaximumSafePlatformFundedDiscount,
   PolicyId }`.
> **Binding (§19.11):** no silent post-checkout change — the decision (incl. any adjustment) is final **before** payment
> is initiated; the caller (P8) shows the final amounts. Engine returns the decision; it does not itself start checkout.

## 7. `ProfitProtectionEvaluationLog` (audit)
Persist every evaluation that results in `ApprovedWithAdjustment`, `Rejected`, or `ConfigurationError` (and optionally
Approved), with the context digest + computed contributions + required + decision + reason. **On failure, NO payment
economics snapshot is created** (snapshot = P8; P5 only logs + returns decision). Insert-only.

## 8. Admin CRUD + persistence
`CreateProfitProtectionPolicy` / `UpdateProfitProtectionPolicy` / `DeactivateProfitProtectionPolicy` with **single-active
overlap guard** (like BE-P2/P4). `IProfitProtectionPolicyRepository` (+ eval-log repo), EF config
(`profit_protection_policies`, `profit_protection_evaluation_logs`; numeric(18,4)/(9,4); unique PolicyCode; effective
index), DbSet, DI. Query `ResolveProfitProtectionPolicy` (dev/admin). Seed one Active default policy (values documented as
placeholders/admin-tunable — do NOT bake business constants).

## 9. Migration (append-only, idempotent)
New tables `profit_protection_policies`, `profit_protection_evaluation_logs` + indexes + default policy seed. No changes
to existing tables. Reversible, duplicate-seed-safe.

## 10. Unit tests (rigorous — this is the heart)
- **Contributions:** each of the three computed correctly from a context; `Required = Max(amount, base×rate)`.
- **Gates:** all pass → `Approved`; each gate failing individually (customer / provider / total) → not Approved.
- **Safe-max:** requested discount > safe-max → `ApprovedWithAdjustment` with applied = safe-max; budget cap applied;
  `CustomerTotal ≥ ProviderNet` preserved; unused right stays (reported).
- **Rejected:** no safe combination meets minimums (even at 0 advantage) → `Rejected`.
- **ConfigurationError:** no active policy / conflicting policies / uncomputable → `ConfigurationError` (+ eval log).
- **Purity/determinism:** engine performs no writes except the eval-log via its repo; same context → same decision.
- **Rounding:** `MoneyMath`; zero-tolerance equality on gates; the loss example from §19.1 (contribution −250) →
  Rejected/Adjustment (not Approved).
- **Policy resolution:** single active; overlap → conflict; none → ConfigurationError.

## 11. Acceptance criteria
- Three contribution gates enforced with **zero tolerance**; `Required` supports amount AND rate; all thresholds from a
  **versioned admin policy** (no hardcoded constants).
- Safe-max platform-funded discount correct (§19.10), budget-capped, keeps `CustomerTotal ≥ ProviderNet`.
- Decision states exactly `Approved/ApprovedWithAdjustment/Rejected/ConfigurationError`; adjustment reduces per
  `AdjustmentOrder`; final-before-checkout (no silent change); failures logged, **no snapshot on failure**.
- Engine pure/context-driven (inputs from P6/P7 arrive via context; not wired into acceptance — that's P8).
- Build clean; migration+seed apply; existing infra untouched.

## 12. Verify — run and PASTE output
1. `dotnet build` Payment: 0 errors.
2. Rebuild + `docker compose up -d --force-recreate payment-api`; clean boot; migration + policy seed applied.
3. DB: `SELECT "PolicyCode","MinTransactionContributionAmount","MinTransactionContributionRate","Status" FROM payment.profit_protection_policies;`
4. Tests green (paste): three contributions, each gate fail, safe-max + budget cap + total≥net, Rejected, ConfigurationError,
   §19.1 loss example → not Approved, purity, policy resolution conflict/none.
5. Smoke: feed a profitable context → Approved; feed the §19.1 loss context → ApprovedWithAdjustment or Rejected with reason.

## 13. Report
`REPORT_BACKEND.md` ("BE-P5"): ProfitProtectionPolicy (versioned) + engine (3 contributions, Required amount|rate,
expected expenses, safe-max discount, decision states, eval log), admin CRUD, migration+seed. Note: discount/benefit
inputs = P6/P7 (context), acceptance wiring + snapshot = P8. Next: **BE-P6 (CustomerDiscountRule + funding +
CustomerBenefitBudget)**. Do not touch CargoDry.
