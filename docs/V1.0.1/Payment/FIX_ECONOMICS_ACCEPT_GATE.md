# FIX (diagnostic-first) — the §19.2 profit-protection accept-gate blocking all dev accepts

> **Repo:** `addesso-project` (Payment). Every offer acceptance in dev is rejected by §19.2 profit-protection
> (**"provider −2.14 / min 0.00", price-independent**) — surfaced in the NF3 smoke, flagged in
> `FIX_PRODUCTION_ECONOMICS_VALUES` + `DECISION_BRIEF_YMM_VAT_COSTSHARE`. **Diagnose the exact cause with live numbers,
> then frame the calibration decision — the fix is a set of policy VALUES, which are the owner's/YMM's call. I do not
> invent them.** Additive/config only. **Do not commit.**

## Confirmed (from code)
- `CustomerSideVariableCostShareRate` = *"fraction of the total expected variable expenses allocated to the CUSTOMER
  side (0..1); the provider side gets the remainder"* (ProfitProtectionPolicyEntity). So **0.25** (Section A; was
  **0.5**) → customer bears **25%**, **provider bears 75%** of the variable expenses (`PaymentProcessingExpenseRate`
  0.0429 + `PaymentProcessingFixed` 0.25 + `RefundRiskReserveRate` + `OtherVariableExpenseRate/Fixed`).
- The engine has **BOTH** a `MinCustomerSideContribution` **and** a `MinProviderSideContribution` (amount + rate). The
  single split rate is a **trade-off**: lowering it (0.25) protects the thin customer side but **starves the provider
  side** — which is exactly the `provider −2.14` breach.
- Accepts **worked at 0.5** (dev-seed-hardening: SR 30009 → Approved) and **fail at 0.25** → the Section A cost-share
  change is the **likely regression**. **"−2.14 price-independent"** points to a **fixed** term dominating (the
  provider's 75% share of `PaymentProcessingFixed` + `OtherVariableExpenseFixed`) and/or the `MinProviderSideContributionAmount`
  floor — not the offer price. **Confirm live.**

## Step 1 — DIAGNOSE (capture the exact breakdown; change nothing)
On a **rejected** accept (a real offer, e.g. provider2 on a fresh SR), capture from the §19.2 evaluation + the active
`profit_protection_policies` row:
1. The **active policy values** — the two mins (customer/provider, amount+rate), the expense rates/fixed, and
   `CustomerSideVariableCostShareRate` (confirm it's 0.25 in the live DB).
2. The **offer economics** — customer total, commission, platform fee, service net.
3. The **computed contributions** — customer-side contribution, provider-side contribution, and the allocated variable
   expenses (total + the 75% provider share, split into rate-based vs fixed). **Which minimum is breached** (provider
   −2.14 vs its min 0.00) and **which term drives the −2.14** (fixed expense share? tiny commission? the min-amount
   floor?).
4. Re-run at **two or three offer prices** (₺1 200 / ₺3 750 / ₺10 000) and record whether the provider-side
   contribution is **truly price-independent** (fixed −2.14) or scales — this tells us if it's a fixed-cost or a
   rate/threshold problem.

## Step 2 — ISOLATE the cost-share (scratch, not shared prod config)
Re-evaluate the **same** accept with `CustomerSideVariableCostShareRate` at **0.5 vs 0.25** — in a **scratch/unit
harness or a throwaway DB**, **not** by mutating the shared `profit_protection_policies` record (the sandbox correctly
blocks that). Confirm: does **0.5 clear** and **0.25 reject**? This isolates whether Section A's 0.25 is the cause vs a
broader mis-calibration.

## Step 3 — FRAME the calibration decision (values = owner/YMM; I don't invent)
Present, with the **diagnosed numbers**, the coherent-calibration trade-off. The whole set must let a **realistic
offer clear BOTH** the customer-side and provider-side minimums:
- **The cost-share split** (`CustomerSideVariableCostShareRate`): 0.25 protects the customer side but starves the
  provider side; 0.5 was the value that cleared. The right value **balances both mins** given the real margins —
  possibly between 0.25 and 0.5, or the split isn't the right lever at all.
- **The expense rates/fixed** (`PaymentProcessingExpenseRate/Fixed`, `RefundRiskReserveRate`, `OtherVariableExpense*`):
  if the provider's 75% share of a **fixed** expense drives −2.14, the fix may be the fixed values (currently
  placeholders — the iyzico `0.0429 + 0.25` is a temporary public-tariff forecast, BSMV-included, per the brief), not
  the split.
- **The two minimums** (`MinCustomerSide` / `MinProviderSideContributionAmount/Rate`): if a min-amount floor is
  unrealistic for thin marine-service margins, that's the lever.
Give the owner a small **what-clears-what** table (cost-share × min-set → decision) from the diagnosed numbers so they
(with the YMM/economics view) pick a **coherent** set. **Do not present an invented value as the answer.**

## Step 4 — APPLY (once decided) + guard the regression
- Set the decided values via the **admin `ProfitProtectionPolicy`** (runtime, P5) and update `ProfitProtectionPolicySeed`
  (fresh DB). Re-verify a realistic accept **clears** end-to-end (economics 200 / Approved → escrow → assignment → the
  NF3 `OFFER_ACCEPTED` push finally fires live).
- **Regression guard:** add a unit/economics test that a **representative realistic offer clears the §19.2 gate on
  both sides** with the seeded policy — so a future single-knob change (like the 0.25) can't silently block all accepts
  again.

## Don't-break / QA
- Diagnosis is read-only; the isolation is scratch/harness (no shared-config mutation). The eventual value change is
  admin/seed config, **no engine-logic change** (the §19.3 "no hardcoded constants" property is preserved). No invented
  production values — the owner/YMM decides; I supply the diagnosed trade-off + the apply path.
- Tests: (1) the breakdown is captured (which min, which term, price-(in)dependence); (2) 0.5-vs-0.25 isolation result;
  (3) after the decided calibration, a realistic accept clears both sides + the accept→assignment→push flow works;
  (4) the regression-guard test passes.

## Report
`docs/V1.0.1/Payment/REPORT_FIX_ECONOMICS_ACCEPT_GATE.md`: the exact −2.14 breakdown (which minimum, which term,
price-dependence), the 0.5-vs-0.25 isolation, the **what-clears-what** table for the owner's calibration decision, and
— once the owner picks the values — the applied policy/seed + the live proof that a realistic accept clears (unblocking
OFFER_ACCEPTED). Update `DECISION_BRIEF_YMM_VAT_COSTSHARE` + [[production_economics_values]] with the outcome.
