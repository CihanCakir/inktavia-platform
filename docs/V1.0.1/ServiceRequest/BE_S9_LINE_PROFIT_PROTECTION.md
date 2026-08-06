# BE_S9 — line-level profit protection (before the transaction-level gates)

> **Repo:** `addesso-project` — implemented in the **Payment module** (SR-numbered, Payment-coded — economics live in
> Payment, like S7/S8; consumed at P8 acceptance). SR second-wave phase S9 (§20.12). Extends the Profit-Protection Engine
> from transaction-level (P5) to **line-level**: **each line must independently clear its own floor before** the §19.2
> transaction gates run — **a line's loss can't be silently hidden in another line's profit.** This is the phase that
> **applies** the S5 `MinimumProviderReceivable` + funded caps that S5 only surfaced.

## Governing rule (§20.12)
Order is **line-level first, then transaction-level.** Line-level controls, per line: `providerMinimumReceivable`,
`allowedProviderFundedDiscount`, `allowedPlatformFundedDiscount`, `commissionFloor`, `linePlatformContribution`, and a
**negative-contribution ban** (a line may run at a loss **only** under an explicit, versioned, limited exception policy —
never silently). Then the existing §19.2 three gates (Customer/Provider/Transaction) run unchanged. **S9 adds a stricter
rejection path; it does NOT change the numbers of an offer that passes** — an approved offer's line/aggregate economics and
the 8-equality are byte-identical.

## Current state (investigated)
- **P5 `ProfitProtectionEngine.Evaluate(ctx, policy)`** is pure + transaction-level (three gates over
  `ProfitProtectionContext`), returning a single `ProfitProtectionEvaluation` + decision
  (Approved/ApprovedWithAdjustment/Rejected/ConfigurationError) with an insert-only `ProfitProtectionEvaluationLog`.
- **`ServiceRequestPaymentEconomicsCalculationService`** (P8 combiner) already builds per-line inputs: S7
  `LineCommissionResolver` → per-line `Commissionable / CommissionBase / ResolvedRate / CommissionAmount / ProviderNet`;
  S6 per-line funded discount; S5 `PartLineAllowanceDto` (cost-free `maxAllowedCustomerDiscount` + funding split +
  `minimumProviderReceivable`) is available via the remote call. Transaction-level `ProfitProtectionEngine.Evaluate` runs
  **after** the lines are built. **S9 inserts the line pass between "lines built" and "transaction Evaluate".**
- P8 already has the gate behaviour: a non-Approved decision → **no snapshot / no escrow / SR acceptance rolled back**.
  S9 reuses this — a line-level failure yields a Rejected/ConfigurationError decision through the same path.

## S9a — line-level thresholds (inputs)
- Per line, resolve the line-level floor set:
  - **Part lines (`Product`/`Consumable`):** `providerMinimumReceivable`, `allowedProviderFundedDiscount`,
    `allowedPlatformFundedDiscount` come from the **S5 `PartLineAllowanceDto`** (already resolved, cost-free) — S9 consumes
    it, it does not re-derive cost.
  - **Non-part lines:** from the **ProfitProtectionPolicy** line-level defaults — extend the policy (versioned, admin-tunable,
    no hardcoded constants) with `DefaultLineMinProviderReceivableRate`/Amount, `DefaultAllowedProviderFundedDiscountRate`,
    `DefaultAllowedPlatformFundedDiscountRate`, `LineCommissionFloorRate`, `MinLinePlatformContributionRate`, and a
    `StrategicLossExceptionEnabled` flag (default **false**). Migration append-only + reseed idempotent.

## S9b — pure `LineProfitProtectionEngine` (Payment domain)
- `LineProfitProtectionEngine.Evaluate(lines, policy, partAllowances)` → per-line `LineProfitProtectionResult` + an overall
  pass/fail. Pure, deterministic, exact decimal (`MoneyMath`), no persistence. For each line compute + check:
  1. **Provider min-receivable:** `line.ProviderNet ≥ providerMinimumReceivable` (part: from S5; else policy). Breach → fail.
  2. **Funded-discount caps:** `line.ProviderFundedDiscount ≤ allowedProviderFundedDiscount` and
     `line.PlatformFundedDiscount ≤ allowedPlatformFundedDiscount`. Breach → fail.
  3. **Commission floor:** `line.ResolvedRate ≥ commissionFloor` (respects the P7 `ProviderCommissionBelowFloor` contract —
     reuse, don't duplicate). Breach → fail.
  4. **Line platform contribution (negative-contribution ban):** `linePlatformContribution = line.CommissionNetRevenue +
     proRataPlatformFeeNetRevenue(line) − line.PlatformFundedDiscount − proRataProviderCommissionBenefitCost(line)`;
     require `≥ minLinePlatformContribution` (≥ 0 by default). The transaction platform fee is attributed **pro-rata by
     line commission base** (documented allocation; reporting-only, does not alter the transaction fee). Breach → **fail
     unless `StrategicLossExceptionEnabled` + within the versioned limit**.
- **No netting:** each line is judged on its own; a positive line never offsets a failing line. First/any breach → overall
  **fail** with a per-line reason.

## S9c — wire into the P8 combiner (order: line then transaction)
- In `ServiceRequestPaymentEconomicsCalculationService`, after the combiner lines are built (S7+S6+S5) and **before**
  `ProfitProtectionEngine.Evaluate` (transaction), call `LineProfitProtectionEngine.Evaluate`. On line-level failure →
  short-circuit to a **Rejected** (or `ConfigurationError` if a policy/allowance is missing) decision with the per-line
  reasons → the existing P8 gate produces **no snapshot/escrow** and SR rolls the acceptance back. On pass → proceed to the
  transaction-level gates exactly as today.
- New error codes in the Payment range (e.g. `LineProfitProtection*` 51xx) for the distinct line breaches.

## S9d — record the per-line result
- Persist the line-level evaluation: extend the **S8 immutable `OfferLineEconomicsSnapshot`** with descriptive line-level
  protection columns (min-receivable applied, line platform contribution, pass flag) **on approval** (self-contained,
  §20.15) — enters no sum/invariant; and/or an insert-only `LineProfitProtectionEvaluationLog` for **non-Approved** cases
  (mirror P5's log, which logs non-Approved without a snapshot). Keep it descriptive; the numbers are unchanged.

## Don't-break / QA
- **Additive + stricter-gate-only:** new line engine + policy line-level fields + combiner wiring + per-line record.
  Existing S1/S6/S7/S8 economics, the transaction-level P5 gates (§19.2), P8 escrow, and the **8-equality are unchanged for
  an offer that passes** — S9 only adds a rejection path (it never changes an approved offer's numbers). Migration
  append-only; policy reseed idempotent; money rules §13.6; exact decimal; UTC-safe. Builds clean.
- Unit tests: (1) an offer where every line clears its floor → **Approved, identical snapshot/8-equality to pre-S9**
  (regression guard); (2) a part line whose `ProviderNet < S5 minReceivable` → **Rejected**, no snapshot/escrow, per-line
  reason; (3) a line whose provider-funded discount exceeds the allowed cap → Rejected; (4) a line below the commission
  floor → Rejected (reuses P7 floor contract); (5) **the netting case — a profitable line + a loss line that pass at
  transaction-level netting must still be Rejected at line-level** (the headline invariant); (6) `StrategicLossException`
  enabled + within limit → the loss line is allowed, logged; disabled → Rejected; (7) missing policy/allowance →
  ConfigurationError, no snapshot.

## Verify
1. An offer with all lines above their floors accepts with the same economics as before S9 (regression).
2. A part line priced below its S5 `MinimumProviderReceivable` → acceptance Rejected, no escrow, per-line reason surfaced.
3. A profitable line cannot rescue a loss-making line: a mix that would net-pass at transaction level is Rejected at
   line-level unless a versioned strategic-loss exception is active.
4. Commission-floor / funded-cap breaches reject with distinct `LineProfitProtection*` codes.
5. Existing transaction-level gates + the 8-equality for a passing offer are unchanged.

## Report
`docs/V1.0.1/ServiceRequest/REPORT_S9.md`: the line-level threshold sourcing (S5 for parts, policy for the rest), the pure
`LineProfitProtectionEngine`, the line-then-transaction ordering in the combiner, the per-line record, the strategic-loss
exception, the **no-netting** proof, the regression proof that a passing offer's 8-equality is unchanged, and the tests.
Then FE (admin policy line-level fields + provider rejection reason surfacing) + next SR phase (S13 dispute / S12 recurring).
**Do NOT commit — leave the working tree for the user to review + commit.**
