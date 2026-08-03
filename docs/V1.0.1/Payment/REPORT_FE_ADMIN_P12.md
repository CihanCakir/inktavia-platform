# REPORT — FE_ADMIN_P12: financial reporting dashboard (P1–P12 finale)

**Status:** ✅ Complete & **verified on-screen** as `admin.user`. Part 1 (BFF) build 0 · Part 2 (FE) typecheck + lint
clean · one required module read-path fix (see below) · KPIs/memos/breakdown/chart/drill-down + a live filter all confirmed
rendering and reconciling to the seeded ledger.

## Required module fix (one line, read-path only)
The "DONE" module endpoint `GET .../reports/ledger-entries` was **broken**: unlike its sibling `financial-summary` (which
does `DateTime.SpecifyKind(..., Utc)`), the ledger-entries action passed the query-string `from`/`to` straight through with
`Kind=Unspecified`, so Npgsql threw `Cannot write DateTime with Kind=Unspecified to 'timestamp with time zone'` → **HTTP
400**, and the drill-down came back empty. Fixed by mirroring the summary endpoint's `SpecifyKind(Utc)` on `from`/`to` in
`PaymentFinanceController.GetLedgerEntries` (read-path parameter normalization — **not** ledger/posting logic). This was a
pre-existing bug the spec's "module is DONE" missed; leaving it would ship a non-working drill-down. `payment-api` rebuilt;
the call now returns **200**.

Vertical slice over the DONE Payment module (`PaymentFinanceController` §15 summary + append-only ledger drill-down). No
module / provider-web / CargoDry changes. Accounting fidelity is preserved end-to-end: VAT liability and provider-funded
discount are **never** folded into revenue / expense / contribution (§19.17).

## Part 1 — BFF passthrough (AdminPanel)
Already present from prior work and verified here (build 0):
- **Remote calls** (`IAdminPaymentBffRemoteCall`): `GetFinancialSummaryReportAsync(from,to,currency)` →
  `FinancialSummaryReportBffDto`; `GetLedgerEntriesAsync(accountLine?,sourceType?,providerProfileId?,from?,to?,currency?,
  page,pageSize)` → `LedgerEntriesPageBffDto`.
- **Typed DTOs** (`AdminFinance/Dto/`): `FinancialSummaryReportBffDto` (From/To/Currency, RevenueTotal [excl. VAT],
  ExpenseTotal, NetMarketplaceContribution [=Revenue−Expense], VatLiabilityTotal [separate], ProviderFundedDiscountTotal
  [separate], `List<LedgerLineBreakdownBffDto>{AccountLine,Nature,Total,EntryCount}`), and `LedgerEntriesPageBffDto`
  (`Items:LedgerEntryBffDto{Id,EntryCode,AccountLine,Nature,Amount,IsReversal,CurrencyCode,SourceType,SourceRef,
  TransactionId,ProviderProfileId,CustomerProfileId,OccurredAtUtc}`, Total/Page/PageSize). Enums reused directly from
  `Payment.Abstraction.Enum`; the DTO shape is mirrored because the module DTOs live in Payment **Application** (not
  Abstraction).
- **Query handlers** (Pattern A, pure passthrough): `GetFinancialSummaryReportBffQuery(Handler)`,
  `GetLedgerEntriesBffQuery(Handler)` — no financial math in the BFF.
- **Controller** (`AdminFinanceController`, `[Authorize(Policy="AdminPanelAccess")]`, `AizenApiResponse` envelope):
  `GET api/v1/admin-panel/finance/reports/financial-summary?from&to&currency` and `…/reports/ledger-entries?…`.
- Verified: `dotnet build Aizen.Bff.AdminPanel` → **0 errors**; both routes respond **401** (up + auth-gated) at
  `localhost:17001`.

## Part 2 — FE dashboard (inktavia-marine-admin-web)
`FinancialReportingDashboardPage` in the existing `pages/app/finance/*` section.

**Wiring** (mirrors the Phase-15 finance section): `shared/api/types/finance.types.ts` (P12 DTOs + filters, enum fields
typed as `number` — the AdminPanel BFF serializes enums as integers, same as the ops-queues); `endpoints.ts`
(`FINANCE_FINANCIAL_SUMMARY`, `FINANCE_LEDGER_ENTRIES`); `queryKeys.ts`; `features/finance/api/financeApi.ts`
(`getFinancialSummary`, `getLedgerEntries` via `httpClient` + `normalizeSuccess/Failure` — envelope-tolerant); hooks
`useFinancialSummaryQuery` / `useLedgerEntriesQuery`.

**Enum label maps** — `features/finance/ledgerEnumMaps.ts` reuses the ops-queues `enumKey` discipline (int → stable i18n
leaf key, tolerant): `LEDGER_ACCOUNT_LINE_KEY` (all 27 §15/§19.17 lines), `LEDGER_NATURE_KEY` (+`_VARIANT` badge),
`LEDGER_SOURCE_TYPE_KEY`, plus ordered option lists for the drill-down filters. No raw ints rendered.

**Page** (`useTranslation('financialReport')`):
- Filters: period (`DateRangePicker`, default last 90 days) + currency (`SelectField`, default TRY) drive both calls.
  Dates expanded to `[from 00:00:00Z, to 23:59:59Z]` UTC.
- Headline KPIs: **Net Marketplace Katkısı** as gold hero (`MetricCard` + gold-glow) · **Toplam Gelir** · **Toplam
  Gider**, with the formula line `Katkı = Gelir − Gider` and a "Gelir KDV hariç" hint.
- Two SEPARATE memos (outside the contribution): **KDV Yükümlülüğü** ("gelir değildir / ayrı yükümlülük") and
  **Sağlayıcı-Finanslı İndirim** ("Inktavia gideri değildir"). Rendered as informational cards, never summed into totals.
- Breakdown grouped by nature: **Gelir kalemleri** (nature=Revenue) / **Gider kalemleri** (nature=Expense) / **Diğer ·
  bilgi** (Liability/Receivable/Memo), each `AccountLine` labeled + `Total` + entry count — no line merged. Plus a
  revenue-vs-expense `BarChartCard` (recharts, reused from `shared/ui/charts`).
- Ledger drill-down: paged audit table over `ledger-entries` — labeled AccountLine, Nature badge, Amount (reversal shown
  as `−amount` + a danger badge), Source, SourceRef, OccurredAt; account-line + source-type filters; pagination.
- **Fidelity guardrail:** the FE never recomputes a total — every figure is read from the summary DTO; VAT + provider-
  funded discount stay outside the contribution.

**i18n:** new `financialReport` namespace, **tr + en full parity** (KPIs, memos, all 27 account-line labels, natures,
source types, chart legends, drill-down columns). Registered in `namespaces.ts` + `i18n.ts`.

**Routing/nav:** `ROUTES.FINANCE_REPORTING_DASHBOARD` = `/app/finance/financial-reporting`; route object added; a nav card
added to the finance hub (`FinanceReconciliationOverviewPage`). Existing reports/finance screens untouched.

**QA:** `npm run typecheck` (tsc) → clean; `eslint` on new/changed files → clean. provider-web / CargoDry git-clean.

## LedgerAccountLine label map (§15 / §19.17 — kept separate, never merged)
Revenue (1–7): ProviderCommission, CustomerPlatformFeeNet, Subscription, ProviderPlan, CustomerPlan, ProviderAddOn,
PremiumProduct. Liability: 10 CustomerPlatformFeeVat. Expense (20–24): PaymentProcessing, GatewayOther, RefundProcessing,
Chargeback, PlatformFundedCustomerDiscount. Memo/benefit (30–31): ProviderFundedCustomerDiscount (memo),
ProviderCommissionBenefitCost. Receivable (40–41). Contribution/settlement (50–55). Expected/actual (60–63). Nature drives
the revenue/expense/other grouping; VAT (10) and provider-funded discount (30) are surfaced only as memos.

## On-screen verification (DONE)
Logged into admin-web (`admin.user@inktavia.com`, OTP via `docker logs -t identity-api | grep DEV-ONLY`) → **Finance →
Financial Reporting Dashboard** (`/app/finance/financial-reporting`), default range 05.05.2026–03.08.2026, TRY. Confirmed:
- **KPIs (both BFF calls 200):** Net Pazaryeri Katkısı **TRY −4,778.00** (gold hero) · Toplam Gelir **TRY 7,222.00** ·
  Toplam Gider **TRY 12,000.00** · formula line "Katkı = Gelir − Gider · KDV yükümlülüğü hariç". Net = 7,222 − 12,000 ✓.
- **Two separate memos, outside the contribution:** KDV Yükümlülüğü **TRY 190.00** ("Gelir değildir…") · Sağlayıcı-Finanslı
  İndirim **TRY 0.00** ("Inktavia gideri değildir…"). Neither folded into any total.
- **Breakdown grouped by nature** (Gelir / Gider / Diğer·Bilgi), every `AccountLine` labeled (tr) + entry count; revenue
  lines sum to 7,222, the single expense line 12,000.
- **Revenue-vs-expense chart** (recharts BarChartCard): Gelir / Gider / Net katkı, with the Net bar rendered below zero.
- **Ledger drill-down** populated (after the module fix): labeled AccountLine, Nature badge (Gelir/Gider/Yükümlülük/
  Alacak/Bilgi), reversal rows shown as **−TRY** + a red "Ters kayıt" badge (e.g. `LED-2-4-1-R` −1,440), labeled Source
  (İade / Kabul Anlık Görüntüsü / Abonelik), Ref, date — reconciling to the summary (commission +1,440+2,280−1,440 = 2,280).
- **Filter round-trip:** Kaynak = İade → `…&sourceType=2…` → 200 → table narrows to exactly the 6 refund-sourced rows.

Values reconcile exactly to the ledger (net-of-reversals): Revenue 7,222 · Expense 12,000 · **Net −4,778** · VAT 190 ·
provider-funded discount 0 — VAT + discount **outside** the contribution (the fidelity requirement, confirmed visually).
No DB mutations were made (drill-down is read-only).

## Wave status & backlog
This completes the P1–P12 FE wave (admin rule-CRUD + conflict fix + provider Wave B + ops queues + cleanup + P11 boost +
keyed-DI fix + **P12 reporting**). Remaining backlog:
- **P9 live iyzico sandbox gate** needs real sandbox keys (`PAYMENT_GATEWAY_ACTIVE=iyzico` + P9 key config).
- **Boost confirm-modal copy** ("Ödeme İyzico üzerinden alınır") is hardcoded — make it gateway-aware (manual gateway
  takes no real charge).
