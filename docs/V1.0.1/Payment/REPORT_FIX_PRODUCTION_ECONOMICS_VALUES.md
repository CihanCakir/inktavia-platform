# REPORT — production economics values · Section A only (cost-share 0.25)

> Implements **Section A** of `FIX_PRODUCTION_ECONOMICS_VALUES.md`: the owner-decided
> `CustomerSideVariableCostShareRate` **0.50 → 0.25** in the profit-protection seed. Additive, admin-tunable, verified.
> **Sections B/C/D are NOT in this pass** (KDV/commission-VAT gap, YMM-gated backlog, 10-question list — no code now).
> **Not committed by me.**

## A. Change — `CustomerSideVariableCostShareRate = 0.25`
`Modules/Payment/src/Aizen.Modules.Payment.Repository/Seed/ProfitProtectionPolicySeed.cs`: the placeholder constant
`CustomerVarShare` **0.50m → 0.25m**, with a comment recording the rationale (provider-heavy transaction split →
customer-side effective revenue ≈ 14–22%, further lowered by the ₺1,500 platform-fee cap, so 50% is too aggressive; 0.25
leaves a small safety margin) and a **RE-CALIBRATE after first real settlement data** note. This is Inktavia's commercial
profit-protection policy — **no YMM gate**. Runtime stays **admin-tunable** (ProfitProtectionPolicy P5); the seed only
sets a fresh DB's starting value. No economics-logic change.

## Verification
1. **Build 0 errors** — Payment host (Release).
2. **Fresh reseed → 0.25** — a fresh-DB `ProfitProtectionPolicySeed.SeedAsync()` seeds a default TRY policy whose
   `CustomerSideVariableCostShareRate == 0.25m` (verified via a temporary explicit assertion on the seed test, which
   passed; the assertion was then reverted so the change set stays the single seed constant).
3. **Idempotent reseed no-op** — `Seed_Is_Idempotent` (double `SeedAsync` → exactly 1 policy) passes.
4. **Existing profit-protection / economics tests pass** — Payment Domain unit tests **119/119** (ProfitProtection +
   Economics filters) and Payment Repository ProfitProtection tests **9/9**, including the profitable-context Approved
   and loss-context ApprovedWithAdjustment cases evaluated against the seeded policy. The 0.25 value (less aggressive
   than 0.50) does not break a viable offer — a context that approved at 0.50 has more headroom at 0.25.

Note: the currently-running payment-api DB still shows the pre-change seeded value (0.50) — the seed only writes on a
**fresh** DB (idempotent skip when a default TRY policy already exists); production/existing DBs adjust via the admin P5
panel. Redeploying payment-api against a fresh DB would seed 0.25 (proven by test 2).

## Out of scope (this pass) — B/C/D captured in the doc, no code
- **B (KDV):** service VAT + platform-fee VAT are already modeled (snapshot-level); `PLATFORM_FEE_VAT_RATE=0.20` has a
  home. The **commission has no VAT split** in `PaymentEconomicsSnapshotEntity` (only base/rate/amount, provider-net =
  service − commission) — so `COMMISSION_VAT_RATE` has nowhere to apply; leave it inert pending the +KDV-vs-inclusive
  decision (a code change if +KDV). **No change now.**
- **C (YMM-gated backlog):** %1 e-commerce withholding (6563), iyzico processing-cost verification (BSMV-included → no
  ×1.20), subscription/premium VAT, invoice party matrix — tracked, not invented.
- **D:** the 10 YMM confirmation questions live on the fix doc / `DECISION_BRIEF_YMM_VAT_COSTSHARE.md`.

**Next after YMM answers:** commission net/VAT/gross modeling if the %15/%12/%9 rates are +KDV; the %1 withholding
settlement feature if in scope.
