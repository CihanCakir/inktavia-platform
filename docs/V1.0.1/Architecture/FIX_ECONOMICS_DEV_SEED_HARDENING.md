# FIX — economics dev-seed hardening (replace the WC1-smoke fudges with proper seed)

> **Repo:** `addesso-project` (Payment module seeders + ReferenceData system-parameter seed). The WC1 smoke reached
> `economics 200 / Approved` only after several **manual dev-DB edits** (`REPORT_FIX_PAYMENT_SYSTEMPARAM_ADAPTER` §5/§8).
> Replace those one-off edits with **proper, idempotent, realistic seed** so a clean `inktavia_store` produces a
> coherent **approving** economics path with **no manual fudging**. Fix the **seeders**, not the runtime rows. Dev-data
> only; two are real seed bugs. **Do not commit.**

## What was fudged (from the report) — and the proper fix per item
### 1. Commission-rule seed — **two real seed bugs** (highest priority)
- The base `commission_rules` (ids 1–8) are seeded with an **empty `RuleCode`**, which economics rejects
  (`ServiceRequestEconomicsCommissionUnresolved`).
- The seed is **non-idempotent**: each payment-api restart inserts **duplicate rows** (16–19), tripping the fail-loud
  conflict guard (`CommissionRuleConflict`).

**Fix:** emit a stable, unique **`RuleCode`** on every canonical base rule in the seeder; make the seeder
**idempotent / duplicate-seed-safe** — upsert by `RuleCode` (or the natural key), so a restart inserts nothing new
and never creates a conflicting duplicate. This matches the project convention *"protect against duplicate seed data."*
Do **not** change the commission **rates** — only the code + idempotency. Verify plan-2 rule 7 resolves unambiguously.

### 2. Profit-protection policy — realistic `CustomerSideVariableCostShareRate`
- The smoke set `profit_protection_policies.CustomerSideVariableCostShareRate = 1.0` (provider bears **no** variable
  cost) purely to clear the decision — explicitly a **non-realistic** value.

**Fix:** seed a **realistic dev default** (aligned with the S9 profit-protection design) that still lets a normal deal
clear — **not** 1.0. The exact figure is a **policy decision**, so: pick a plausible dev default, mark it clearly as a
dev default in the seed comment, and ensure the **seeded test offer economics** (offer amount + commission + platform
fee + VAT) produce a **positive provider-side contribution** with that realistic rate — i.e. make the dev economics
set *coherent*, not just flip one knob. If a coherent realistic set can't clear the current test offer, adjust the
**test offer/seed amounts** (dev data) rather than distorting the policy rate. **Flag the chosen value for the owner to
confirm** — do not present an invented number as the production policy.

### 3. Provider payment profile + clean balance
- Test provider profile **100011** had no `payment.provider_payment_profiles` row (not split-eligible) and a stale
  **`provider_balances` = -10560** (from prior test refunds, over the 5000 limit → blocked).

**Fix:** seed a durable **Verified, keyed, IBAN-present** `provider_payment_profiles` row for the test provider(s)
(mirror `SubMerchantOnboardingMockSeed`), and ensure test providers **start at a clean balance** (0/positive) — the
seed should not leave stale negative balances. Make it idempotent.

### 4. VAT/KDV system parameters — seed a dev default (real value = R2/YMM)
- `PLATFORM_FEE_VAT_RATE` / `COMMISSION_VAT_RATE` are **unseeded**; economics falls back to the code default `0.20`.

**Fix:** seed both keys in `ref.SystemParameters` with a **dev default `0.20`** (TR standard KDV), clearly commented as
a **placeholder pending the real R2/YMM values** — so economics reads a seeded parameter instead of a silent code
default. **Do not present 0.20 as the production rate** (which services are 0.20 vs 0.10 vs 0.01 is a YMM decision);
this only removes the "unseeded" gap. Duplicate-seed-safe.

## ⚠️ Separate functional gap (NOT this seed — flag only)
**Auto-create the provider assignment on offer-accept.** Owner-accepted SRs (55/56) leave the SR at `OfferAccepted`
with **no assignment**, so they never appear in the provider's Jobs — the smoke had to drive `JOB_STARTED`/
`JOB_COMPLETED` on a **pre-assigned** seed job (SR 9011). That is a real **workflow gap** (accept should hand the
provider a job), not a seed issue. Recommend a **separate kickoff** (owner accept → create the `ServiceRequestAssignment`
so the accepted job surfaces in the provider Jobs list). Do **not** fold it into this seed task.

## Don't-break / QA
- **Seeders only** (dev-data); no economics/commission/profit-protection **logic** changes; commission **rates**
  unchanged. All seeds **idempotent** (restart → 0 new rows, no conflicts). No production values invented (VAT + the
  cost-share rate are marked dev defaults / flagged for owner confirmation).
- Tests / verification: (1) Payment + ReferenceData + solution build 0 errors; (2) **wipe → reseed** `inktavia_store`
  → commission rules have RuleCodes + **no duplicates after N restarts**; (3) provider payment profile Verified +
  balance clean; (4) VAT keys present (0.20 dev); (5) profit-protection realistic rate seeded; (6) **owner accept →
  economics 200 / Approved with NO manual DB edits** (the whole point) on a freshly-seeded DB — reproduce the WC1
  accept path clean; (7) commission conflict guard not tripped on restart.

## Report
`docs/V1.0.1/Architecture/REPORT_FIX_ECONOMICS_DEV_SEED_HARDENING.md`: the commission-rule seed fix (RuleCodes +
idempotency), the realistic profit-protection rate (value + why + **owner-confirm flag**), the provider payment
profile + clean-balance seed, the VAT dev-param seed (pending R2/YMM), and the **clean-DB owner-accept-200 proof with
no manual edits**. Re-flag the assignment-on-accept workflow gap as a separate kickoff. Then **WC2** (chat write
cutover) — WC1 + its seed debt fully closed.
