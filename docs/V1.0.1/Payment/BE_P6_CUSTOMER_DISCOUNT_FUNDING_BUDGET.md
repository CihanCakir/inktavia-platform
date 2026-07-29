# BE-P6 — `CustomerDiscountRule` + Funding + `CustomerBenefitBudget` (reserve/consume/release) — Backend Prompt

> **Module:** `Aizen.Modules.Payment`. **Phase:** Payment P6 (roadmap `docs/V1.0.1/Payment/ROADMAP.md`).
> **Canonical:** `docs/V1.0.1/COMMISSION_PACKAGE_PRICING.md` §19.6 (CustomerDiscountRule + funding), §19.7 (benefit
> budget), §8/§19.15 (concurrency). Produces the P5-engine inputs `RequestedPlatformFundedCustomerDiscount`,
> `ProviderFundedCustomerDiscount`, `CustomerBenefitBudgetRemaining`, `CustomerPlanRevenueAllocation`.
> **Rule:** EXTEND; do NOT rewrite. Reuse BE-P2..P5 versioned/effective-date + fail-loud + `MoneyMath` + resolver
> conventions. Inspect first.

## 0. Verified current state (KEY reconciliation)
- No `CustomerDiscountRule`/`CustomerBenefitBudget` infra → create.
- **`ParticipantPlanEntity` ALREADY has `ServiceDiscountRate`** (e.g. 0.05) + `CargoDryDiscountRate`, and
  `ParticipantPlanSubscriptionEntity` snapshots `ServiceDiscountAtSubscription`. So a **plan-level customer service
  discount already exists.** → Reconcile (below), do not create a conflicting parallel discount.
- `CustomerBenefitBudget` ties to `ParticipantPlanSubscriptionEntity` (customer subscription) — it exists.
- P5 `ProfitProtectionContext` already declares the fields P6 must populate (RequestedPlatformFundedCustomerDiscount,
  ProviderFundedCustomerDiscount, CustomerBenefitBudgetRemaining).

## 1. Reconciliation of the existing plan `ServiceDiscountRate`
- `CustomerDiscountRule` becomes the **authoritative** customer-discount model (category + plan + campaign, funding,
  bounds). The existing plan `ServiceDiscountRate` is a **PlatformFunded, plan-scoped percent service discount** →
  **seed** each plan's `ServiceDiscountRate` as a `CustomerDiscountRule(CustomerPlanId=X, DiscountType=Percent,
  DiscountRate=<plan rate>, FundingMode=PlatformFunded, Status=Active)`.
- Keep `ParticipantPlan.ServiceDiscountRate` + `ServiceDiscountAtSubscription` as **legacy/display** (like
  `ProviderPlan.MonthlyPriceTRY` in P4) — do not break them.
- **Timing (open decision, document):** the old model **locks** the plan discount at subscription
  (`ServiceDiscountAtSubscription`); the new model resolves `CustomerDiscountRule` **at transaction time**. P6 resolves at
  transaction time (needed for funding + profit protection); flag whether the plan-scoped rule should be capped by the
  subscription snapshot — leave as open decision, default = resolve-at-transaction.

## 2. `CustomerDiscountRule` entity (§19.6)
`Payment.Domain/Entities/CustomerDiscount/CustomerDiscountRuleEntity.cs : AizenEntityWithAudit`:
`CustomerPlanId?`, `CategoryCode?`, `DiscountType` (Percent/Fixed), `DiscountRate?`, `FixedDiscountAmount?`,
`MinimumPurchaseAmount?`, `MaximumDiscountAmount?`, `EffectiveFrom`, `EffectiveTo?`, `Priority`, `FundingMode`
(`PlatformFunded/ProviderFunded/Shared`; enum reserves `SupplierFunded` for future), `PlatformFundingRate?`,
`ProviderFundingRate?` (Shared → sum == 1.0), `RequiresProviderConsent` (bool, default true for ProviderFunded),
`CurrencyCode`, `Status`, `RuleCode` (unique), `RuleName?`. Validation: model coherence (Percent needs Rate; Fixed needs
FixedAmount; Shared needs funding rates summing to 100%); **binding: no rule without a funding source.**

## 3. Resolution + requested-discount calc (pure)
- `ResolveCustomerDiscountAsync(ctx: CustomerPlanId?, CategoryCode?, CurrencyCode, atUtc) → CustomerDiscountResolution?`
  (pure, specificity + fail-loud conflict — mirror BE-P2: `CustomerPlan+Category > CustomerPlan > Category > Global`,
  then Priority, tie → `CustomerDiscountRuleConflict`).
- **Requested discount** = apply rule to the service base: Percent → `Round(base × DiscountRate)`; Fixed →
  `FixedDiscountAmount`; enforce `MinimumPurchaseAmount` (rule inert below it) and clamp to `MaximumDiscountAmount`.
- **`ResolveCustomerDiscountFundingAllocation`** splits the requested discount into
  `{ PlatformFundedAmount, ProviderFundedAmount, SupplierFundedAmount(0 for now) }` per `FundingMode` (Shared → by
  funding rates). **Binding funding rules (§19.6):** platform discount NOT auto-shifted to provider (or vice-versa);
  ProviderFunded requires provider consent (input flag; capture flow deferred) — without consent, provider-funded portion
  is **not applied** (reduce/omit, never silently platform-fund it).

## 4. `CustomerBenefitBudget` + reservation/consumption (§19.7)
- `CustomerBenefitBudgetEntity`: `ParticipantPlanSubscriptionId`, `CustomerPlanId`, `PeriodStart`, `PeriodEnd`,
  `FundedAmount`, `ReservedAmount`, `ConsumedAmount`, `RemainingAmount` (= Funded − Reserved − Consumed), `CurrencyCode`,
  `Status`, **`Version`** (optimistic concurrency).
- `CustomerBenefitBudgetPolicy` (per plan, versioned): `BenefitBudgetRate` (fraction of plan revenue → budget; **ELITE ≠
  unlimited**, §19.7-6), `PerPeriodMax?`, `PerCategoryLimit?`, `PerTransactionLimit?`, `RefundRestorePolicy`
  (Restore/Consume). Budget instance per subscription period: `FundedAmount = planPeriodRevenue × BenefitBudgetRate`.
- `CustomerBenefitReservationEntity`: `BudgetId`, `Amount`, `Status` (Reserved/Consumed/Released), `ContextRef`
  (offer/txn), timestamps.
- Lifecycle: **reserve** (before checkout, decrements Remaining) → **consume** (on successful payment) or **release** (on
  failure/timeout/cancel). Discount cannot exceed `RemainingAmount`. **Concurrency (§8/§19.15):** optimistic concurrency
  on `Version` + unique constraint so the **same budget cannot be double-reserved/consumed by two concurrent checkouts**;
  duplicate webhook cannot double-consume.
- This is **not a wallet** — a commercial-advantage control budget only.

## 5. P5-engine input production
Provide a service method that assembles for a given (customer, category, offer base) the fields P5 needs:
`RequestedPlatformFundedCustomerDiscount`, `ProviderFundedCustomerDiscount`, `CustomerBenefitBudgetRemaining`,
`CustomerPlanRevenueAllocation` (customer plan revenue allocation for the customer-side contribution). Actual wiring into
acceptance + reserve/consume ordering = P8; P6 provides the resolvers + budget ops + a `ReserveBenefit`/`ConsumeBenefit`/
`ReleaseBenefit` command set.

## 6. Admin CRUD + persistence
- `CustomerDiscountRule` CRUD (conflict guard, funding validation) · `CustomerBenefitBudgetPolicy` CRUD ·
  budget/reservation read + manual adjust (audit). Repos (pure resolve; budget ops with concurrency), EF configs
  (`customer_discount_rules`, `customer_benefit_budgets`, `customer_benefit_reservations`, `customer_benefit_budget_policies`;
  numeric(18,4)/(9,4); unique codes; `Version` rowversion; indexes), DbSets, DI.

## 7. Migration (append-only, idempotent)
New tables above + **seed**: CustomerDiscountRule from each `ParticipantPlan.ServiceDiscountRate` (PlatformFunded,
plan-scoped) + a default CustomerBenefitBudgetPolicy per plan (rate documented as placeholder/admin-tunable). No
destructive change; legacy plan fields kept. Idempotent.

## 8. Tests
- **Resolution/specificity/conflict:** CustomerPlan+Category > CustomerPlan > Category > Global; tie →
  `CustomerDiscountRuleConflict`; MinimumPurchase inert; MaximumDiscount clamp.
- **Funding:** PlatformFunded / ProviderFunded (consent yes/no — no consent → provider portion not applied, not
  auto-platform-funded) / Shared (rates sum 100%, split correct); no-funding-source rule rejected at create.
- **Budget:** reserve decrements Remaining; consume on success; release on failure; discount > Remaining rejected;
  **concurrent reserve on same budget → one succeeds (optimistic concurrency)**; duplicate consume idempotent; ELITE
  budget = FundedAmount cap (not unlimited).
- **Reconciliation:** seeded plan discount resolves as PlatformFunded plan-scoped rule matching `ServiceDiscountRate`.
- **Rounding:** `MoneyMath`; clamp/min behavior exact.

## 9. Acceptance criteria
- CustomerDiscountRule authoritative; existing plan `ServiceDiscountRate` reconciled via seed (legacy fields kept);
  timing (resolve-at-transaction) documented as open decision.
- No discount without a funding source; funding split correct; ProviderFunded needs consent (no silent platform-funding).
- Benefit budget bounded (ELITE ≠ unlimited), reserve→consume/release, **concurrency-safe** (no double spend), refund
  restore per policy (restore hook wired in P10).
- Produces the P5-engine inputs; wiring into acceptance = P8. Build clean; migration+seed apply; existing infra untouched.

## 10. Verify — run and PASTE output
1. `dotnet build` Payment: 0 errors.
2. Rebuild + `docker compose up -d --force-recreate payment-api`; clean boot; migration + seed applied.
3. DB: `SELECT "CustomerPlanId","DiscountType","DiscountRate","FundingMode","Status" FROM payment.customer_discount_rules;`
   (seeded plan discounts, PlatformFunded) + `customer_benefit_budget_policies` rows.
4. Tests green (paste): specificity+conflict, funding modes + consent, budget reserve/consume/release + **concurrent
   reserve one-wins**, discount>Remaining rejected, ELITE cap, reconciliation seed, rounding.
5. Smoke: resolve a plan+category discount → amount + funding split; reserve against budget → Remaining decrements;
   concurrent reserve → conflict/one-wins.

## 11. Report
`REPORT_BACKEND.md` ("BE-P6"): CustomerDiscountRule + funding allocation + CustomerBenefitBudget (reserve/consume/release,
concurrency) + reconciliation of plan ServiceDiscountRate (seed) + admin CRUD + migration. Note: order-level voucher
line allocation = ServiceRequest S6; provider-consent capture flow deferred; acceptance wiring + reserve/consume ordering
= P8; refund restore = P10. Next: **BE-P7 (ProviderCommissionBenefitRule)**. Do not touch CargoDry.
