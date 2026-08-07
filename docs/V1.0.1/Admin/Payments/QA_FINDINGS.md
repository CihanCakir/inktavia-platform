# Admin QA — Payments & Commissions (A-QA2)

Pages: `src/pages/app/payments/*` + `CommissionPayoutsPage.tsx`. Feature: `src/features/payments/{api,hooks,queues,rule-crud}`.
Enum mappers: `src/features/payments/queues/enumMaps.ts`. Sidebar: `DashboardLayout.tsx` (NOT navigation.ts).

## Static findings
Most payment screens are **REAL + wired** to `paymentApi` hooks — the P1–P12 economics surface (commission rules, platform
fee, customer discount, profit protection, commission-benefit rules/entitlements, benefit budget, part commercial terms,
refund/chargeback queues, provider balances/payouts, refund-allocation policies, sub-merchant KYC). Queues use `enumMaps`
correctly. Money defaults TRY. But:

**Mock/stub screens despite live backend (X5):**
- **Gateway Logs** `PaymentGatewayLogsPage.tsx` (L9-41) — **100% mock, no hooks** (live-confirmed: 14,282/142/28, fake
  "Stripe Marine / Marine Pay / Global Swift" gateways, EVT_99xx, 2026-06-30). Marketplace uses **iyzico** → fake Stripe
  branding is a go-live risk. Comment: "PaymentWebhookLog not yet implemented".
- **Entitlements Monitoring** `EntitlementMonitoringPage.tsx` (L9-45) — 100% mock (`MOCK_ENTITLEMENTS/MATRIX`).
- **Commission Calculation detail** `CommissionCalculationDetailPage.tsx` (L9-40) — 100% mock; captures then discards
  `:calculationId`.

**Currency (X3):** `PaymentTransactionDetailPage.tsx` mock rows `currencyCode:'USD'` (L25,34) + hardcoded 2.75% gateway fee
(L157); `CommissionRulesPage.tsx:460` placeholder "e.g. USD"; i18n `amountUsd` key. Rule-CRUD pages correctly default TRY.

**Numeric-enum risk (X4):** rule-CRUD list pages + `PaymentTransactionsPage`/`ProviderPayoutsPage`/`UserSubscriptionsPage`
render `{row.status}` via **string-keyed** maps; several `payment.types.ts` fields are typed `number` (e.g.
`CommissionRuleDto.status`, refund `status`). If the BFF emits ints, the badge blanks / a raw number leaks. `enumMaps` is
only applied on the queue pages. **Verify live payloads.** `PaymentTransactionsPage.tsx:129`
`transactionType.replace(...)` will **throw** if `transactionType` arrives numeric.

**Legacy `/app/commissions`** `CommissionPayoutsPage.tsx` — orphaned (not in any sidebar), calls likely-404
`/commissions/summary`, **dead "Process payout" button** (no onClick), hardcoded English despite `commissions.json`, raw
`{currency}` money.

## Live walkthrough checklist
- [ ] Gateway Logs / Entitlements / Commission-Calc-detail: real data or hidden — no fake Stripe/mock arrays shipped.
- [ ] Every rule-CRUD list + transactions/payouts/subscriptions: status badges render **labels** (not raw ints/blank).
- [ ] All money ₺; transaction detail not USD; gateway fee real not hardcoded 2.75%.
- [ ] `/app/commissions` removed or rewired; no dead payout button.
- [ ] Refund/chargeback/KYC/balances/allocation queues load with correct enum labels + ₺.

## Fix candidates
`FIX_A_QA2_PAYMENT_MOCK_SCREENS` (wire or hide the 3 mock pages + Stripe branding), `FIX_A_QA2_ENUM_PAYLOADS` (verify + map
rule-list statuses), retire `/app/commissions`. **Gated:** live gateway realism needs iyzico keys (P9).
