# REPORT — §19.2 accept-gate diagnostic (the "provider −2.14" rejection)

> Diagnostic-first per `FIX_ECONOMICS_ACCEPT_GATE.md`. Read-only + a scratch unit harness (deleted). **No shared-config
> mutation. Nothing committed.** **Headline: the −2.14 is NOT a profit-protection calibration problem — it's an offer
> with un-priced lines (a ₺0 service). No owner/YMM value decision is needed; the fix is a code fix.**

## Step 1 — the exact §19.2 breakdown (captured live)

**Active TRY policy** (`payment.profit_protection_policies` id 1): `CustomerSideVariableCostShareRate = **0.50**` (NOT the
0.25 the doc assumed — the running dev DB never took the Section-A seed change), `PaymentProcessingExpenseRate 0.029` +
`PaymentProcessingFixed 0.25`, `RefundRiskReserveRate 0.005`, `Other* 0`; mins: customer 0/0, provider **0/0**,
transaction **10 / 0.01**.

**Rejection (byte-identical at ₺1 200 / ₺5 000 / ₺10 000 — re-run live):**
`customer 96.85 / 0.00 · provider **−2.14 / 0.00** · transaction 94.71 / 10.00 → Rejected`
- **Which minimum:** the **provider-side** gate. `providerContribution = −2.14 < reqProvider = 0.00`. (Customer +96.85 ≥ 0
  and transaction 94.71 ≥ 10 both pass.)
- **The engine** (`ProfitProtectionEngine.cs`, absolute-TRY amounts): `provider = ProviderCommissionNetRevenue − benefit −
  providerVarCost`, `providerVarCost = totalExpenses − Round(totalExpenses × 0.5)`, `totalExpenses = Round(CustomerTotal
  × 0.034) + 0.25`.
- **Which term drives −2.14:** with **CustomerTotal = 118.80** (see below), `totalExpenses = Round(118.80×0.034)+0.25 =
  4.29`, `providerVarCost = 4.29 − Round(4.29×0.5)=2.15 = **2.14**`, and `ProviderCommissionNetRevenue = **0**` → `0 − 2.14
  = −2.14`. **The provider's ₺0 commission cannot cover its ₺2.14 share of the (platform-fee-floor-driven) expenses.**
- **Price-independence — the real tell:** the offer's `UnitPrice` (1 200/5 000/10 000) **never enters the economics**. The
  offer lines are **un-priced** (`LineSubtotal = TaxAmount = CommissionBaseAmount = 0`, confirmed in
  `service_request_offer_items` for offers 21/22/23), so the engine sees a **₺0 service**. The only revenue is the
  **platform-fee minimum floor ₺99** (`PlatformFeeRule` = 2.5% bounded **[₺99, ₺1500]**), giving feeGross 118.80 →
  `CustomerTotal = 0 + 118.80 = 118.80` regardless of the quoted price. Hence the identical −2.14.

## Step 2 — cost-share 0.5-vs-0.25 isolation (scratch harness, real engine)

Ran the actual `ProfitProtectionEngine.Evaluate` in a throwaway unit harness (no shared-config change), degenerate vs
realistic offer × cost-share 0.5/0.25:

| scenario | cost-share | customer | provider | transaction | reqTxn | decision |
|---|---|---|---|---|---|---|
| **degenerate ₺0-svc** (un-priced lines) | 0.50 | 96.85 | **−2.14** | 94.71 | 10.00 | **Rejected** |
| degenerate ₺0-svc | 0.25 | 97.93 | **−3.22** | 94.71 | 10.00 | **Rejected** |
| **realistic ₺1 000-svc** (priced) | 0.50 | 76.45 | **+127.46** | 203.91 | 13.19 | **Approved** |
| realistic ₺1 000-svc | 0.25 | 87.73 | **+116.18** | 203.91 | 13.19 | **Approved** |

The degenerate 0.50 row **reproduces the live −2.14 exactly** (validates the reconstruction against the real engine).
**Conclusions that overturn the doc's hypothesis:**
- **Cost-share is not the lever.** 0.5 does not "clear" and 0.25 does not "reject" — the degenerate offer fails at **both**
  (0.25 is worse: −3.22), and a realistic offer **passes at both** (+127 / +116).
- The live value is **0.5**, and 0.5 already rejects the degenerate offer — so the "0.25 regression" theory is moot.

## Step 3 — what-clears-what (the calibration decision that isn't needed)

The table above **is** the what-clears-what, and its lesson is: **no policy value change clears the degenerate offer, and
no policy value change is needed for a real one.** The active calibration (cost-share 0.5, commission 15%, platform fee
2.5%/[99,1500], mins 0/0/[10,1%]) **correctly approves realistic offers** (provider +127 at 0.5). The gate is doing its
job — it's rejecting a genuinely unprofitable ₺0-service offer.

**Live corroboration:** the already-**accepted** seed offers (SR 30009 offer 50007, SR 9011 offer 90001 — status
`Accepted`) have **real** priced lines (`LineSubtotal` 3000/750/1800…) — priced offers clear the gate in this same DB.

## Root cause (the actual bug) + fix path

`OfferCalculationService` computes `LineSubtotal / TaxAmount / CommissionBaseAmount`, and it is invoked by
**`SaveOfferDraft` / `SubmitOffer` / `PreviewOffer`** — but **`CreateServiceRequestOfferCommandHandler` (`POST
/provider/service-requests/{id}/offers`) never calls it**, so offers created via that endpoint have zero line economics.
Provider-web has **both** flows: the offer *builder* uses draft→submit (priced), but **`ServiceRequestDetailPage` uses the
bare `createOffer` endpoint** (un-priced) — so this is not only my NF1–NF3 test artifact; a provider offering from that
page produces a degenerate, un-acceptable offer.

**Recommended fix (code, not config — no owner/YMM decision):**
1. **Price the lines on create:** have `CreateServiceRequestOffer` run the same `OfferCalculationService` pass as
   `SubmitOffer` (compute `LineSubtotal/TaxAmount/CommissionBaseAmount`), **or** route `ServiceRequestDetailPage` through
   the draft→submit flow. Then a real offer carries real economics and the accept clears.
2. **Regression guard (Step 4):** a Payment economics test that a representative **realistic** offer clears §19.2 on both
   sides, and (SR-side) that an offer cannot be submitted/accepted with zero `CommissionBaseAmount`/`LineSubtotal` — so a
   future change can't silently ship un-priced offers.
3. **No `ProfitProtectionPolicy` value change.** (Attempting to relax the shared policy to force the accept was correctly
   blocked by the sandbox; it would also have been the wrong fix.)

**This unblocks OFFER_ACCEPTED** (NF3): a properly-priced offer accepted → economics 200/Approved → escrow → assignment →
the `OFFER_ACCEPTED` push fires — no economics recalibration required.

## Notes
- The doc's premise (all dev accepts rejected by a cost-share mis-calibration) is **not** what's happening; update
  `DECISION_BRIEF_YMM_VAT_COSTSHARE` / [[project_fix_production_economics_values_a]]: the 0.25-vs-0.5 question is real for
  margin policy but is **not** the accept-gate blocker. The accept-gate blocker is un-priced offer lines.
- No shared config mutated (policy still 0.50 / provider-min 0). Scratch harness deleted. Nothing committed.
