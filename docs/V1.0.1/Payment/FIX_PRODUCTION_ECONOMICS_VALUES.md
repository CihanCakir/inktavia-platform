# FIX — production economics values (cost-share 0.25 + KDV) + verified readiness gaps

> **Repo:** `addesso-project` (Payment). Apply the owner's decided values from the YMM/KDV research and record the
> **code-verified** findings that shape what can actually go live. **One value is applicable now; the KDV work has a
> real code gap; two items are YMM-gated.** Additive. **Do not commit.**

## A. APPLICABLE NOW — `CustomerSideVariableCostShareRate = 0.25` (business decision, no YMM gate)
The owner decided **0.25** (from unit-economics: the transaction revenue split is provider-heavy — customer-side
effective revenue ≈ %14–22 across plans, and the ₺1.500 platform-fee cap lowers it further — so loading **50%** of
variable cost onto the customer-side contribution is too aggressive; **0.25** leaves a small safety margin).
- **Change:** `ProfitProtectionPolicySeed.cs` — `CustomerSideVariableCostShareRate` placeholder **0.50 → 0.25**.
- Runtime is **admin-tunable** (ProfitProtectionPolicy P5), so production can also be set/kept via the admin panel; the
  seed just makes a fresh DB start at the decided value. No YMM gate — this is Inktavia's commercial profit-protection
  policy.
- **Re-calibrate** after the first real settlement data (actual processing cost, refund/chargeback rate, transaction
  mix). Note this in the seed comment.

## B. KDV — verified: platform-fee works, **commission has NO home** (the real blocker)
Verified against `PaymentEconomicsSnapshotEntity`:
- **Service VAT is already modeled** at the snapshot level — `ServiceAmountSnapshot` / `ServiceVatAmountSnapshot` /
  `ServiceGrossAmountSnapshot`, **transaction-level** (not a global rate). This matches the research's recommendation
  (**do not** create a single global `SERVICE_VAT_RATE`; keep per-transaction / a `ServiceTaxPolicy`). **No action.**
- **Platform fee VAT is modeled** — `PlatformFeeNet/Vat/GrossAmountSnapshot`. `PLATFORM_FEE_VAT_RATE = 0.20` has a home
  and works. Keep the system-param at **0.20** (already the dev value); **YMM sign-off gates the value**, not the code.
- **⛔ Commission has NO VAT split** — the snapshot carries only `CommissionBaseAmountSnapshot` /
  `CommissionRateSnapshot` / `CommissionAmountSnapshot`, and `ProviderNetAmountSnapshot = ServiceAmount −
  CommissionAmount`. There is **no `CommissionVat/Net/GrossAmountSnapshot`**. So **`COMMISSION_VAT_RATE = 0.20` has
  nowhere to apply** — the commission is currently a **flat deduction** (implicitly VAT-inclusive). This is exactly the
  "commission net/VAT semantics" the research flagged, confirmed in code.
  - **Decision required first (contract + UI):** are the `%15 / %12 / %9` rates **VAT-exclusive (+KDV)** or
    **VAT-inclusive**? If **+KDV**, the snapshot must gain `CommissionNet/Vat/Gross` (like platform fee) and the
    provider-net derivation + invoicing must use them — a **code change**, not just a seed value. If **inclusive**, the
    current model stands but the platform's commission-KDV for invoicing is implicit (confirm the invoice engine splits
    it). **Do not set `COMMISSION_VAT_RATE` as meaningful until this is decided** — leave the param at 0.20 as a
    placeholder with a comment that it is inert pending commission-VAT modeling.

## C. YMM-GATED / new backlog (from the research — do NOT invent; capture as tracked items)
1. **%1 e-commerce withholding (tevkifat, 2025)** — if the YMM classifies Inktavia as an *"elektronik ticaret aracı
   hizmet sağlayıcı"* (6563), provider payouts require a **1% income/corporate-tax withholding** on the KDV-exclusive
   service base. **New settlement feature** (fields `ProviderGrossEntitlement / ECommerceWithholdingBase / Rate /
   Amount / ProviderCashPayout`). This is **not** platform revenue and **not** KDV. Gated on the YMM/legal
   classification → then a settlement-policy + code kickoff. **Highest-impact open item.**
2. **iyzico processing cost** — `PaymentProcessingExpenseRate = 0.0429` + `Fixed = 0.25 TRY` are **temporary** public-
   tariff forecasts (**BSMV-included → do NOT multiply by 1.20**). **Verify** the profit-protection variable-cost calc
   uses the rate **as-is** (no ×1.20 VAT). Real values come from the signed iyzico merchant contract + actual
   settlement reports (the existing `PaymentProcessingExpenseActual` / reconciliation fields are the right home).
3. **Subscription VAT + Premium/Boost (digital) VAT** = **0.20** proposed → new system-params, YMM sign-off.
4. **Invoice party/flow matrix** — provider→customer (service), Inktavia→provider (commission), Inktavia→customer
   (platform fee); iyzico is the payment layer, not the invoice issuer. YMM + legal confirm against the
   Inktavia–provider contract.

## D. The 10 YMM confirmation questions
Carry the research's 10 questions to the YMM (commission %20; %15/%12/%9 net-vs-gross; platform fee net + %20;
provider→customer service invoicing; marine service VAT exemptions → transaction-level; subscription/boost %20; 6563
e-ticaret classification; who computes/declares the %1; withholding base = KDV-exclusive service; refund/cancel
KDV + withholding correction procedure). Store the answers on this doc.

## Don't-break / QA
- **B-only code change now = the cost-share seed 0.25** (additive, idempotent, admin-tunable). No economics logic
  change. KDV values unchanged (0.20 placeholders). No commission-VAT modeling in this pass (gated on the net-vs-gross
  decision). No withholding code (gated on YMM).
- Tests: (1) Payment build 0 errors; (2) fresh reseed → default ProfitProtectionPolicy has `CustomerSideVariableCostShareRate
  = 0.25`; (3) idempotent reseed no-op; (4) existing economics/profit-protection tests still pass (0.25 doesn't break a
  seeded viable offer — re-check the dev test offer clears, or adjust the test offer, per the dev-seed-hardening note).

## Report / status
`docs/V1.0.1/Payment/REPORT_FIX_PRODUCTION_ECONOMICS_VALUES.md`: the cost-share 0.25 seed change, the verified KDV state
(service + platform-fee modeled; **commission-VAT snapshot gap** is the blocker for `COMMISSION_VAT_RATE`), and the
YMM-gated backlog (e-commerce withholding, iyzico contract, subscription/premium VAT, invoice matrix) with the 10 YMM
questions. **Next after YMM answers:** (1) commission net/VAT/gross modeling if rates are +KDV; (2) %1 withholding
settlement feature if in scope. The decision brief `DECISION_BRIEF_YMM_VAT_COSTSHARE.md` is the living record — update
its status table as answers land.
