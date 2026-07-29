# BE-P12 — Financial reporting ledger (revenue/expense/contribution lines) + period reports — Backend Prompt

> **Module:** `Aizen.Modules.Payment`. **Phase:** Payment P12 (roadmap `docs/V1.0.1/Payment/ROADMAP.md`) — **the reporting
> capstone.**
> **Canonical:** `docs/V1.0.1/COMMISSION_PACKAGE_PRICING.md` §15 (15 ledger lines + NetMarketplaceContribution), §19.17
> (13 additional distinctions + rules), §13.10 (PlatformGrossShare / PlatformSettlementNetAmount).
> **Rule:** NEW append-only reporting ledger, **posted from the existing immutable sources** (PaymentEconomicsSnapshot,
> RefundAllocation, ChargebackRecord, PremiumPurchase, subscriptions) so every line is **reconcilable** and **derived, not
> recomputed**. Do NOT change any economics/refund/premium logic — reporting is additive. Reuse `MoneyMath` + the existing
> read-query pattern (`GetFinanceInvoiceStatementReport`). Inspect first. Do NOT touch CargoDry.

## 0. Verified current state
- **No financial ledger / reporting-account infra → create.** Sources already immutable/persisted:
  `PaymentEconomicsSnapshot` (+ line/discount/commission children, P1/S8/P8b), `RefundAllocation` + `ProviderBalance`/
  movements + `ChargebackRecord` (P10), `PremiumPurchase`/`PremiumEntitlement` (P11), provider/participant subscriptions.
- Event hooks to post from: acceptance (P8 `LinkEconomicsSnapshot` / snapshot created), refund (`RefundPaymentCommandHandler`
  / `ApplyRefund`), chargeback (`RecordChargebackCommand`), premium paid (`PremiumBoostService.OnBoostPaidAsync`) + refund
  (`OnBoostRefundedAsync`), subscription charge (`SubscribeProviderPlan`/`SubscribeParticipantPlan`).
- `GetFinanceInvoiceStatementReport` = the read-report pattern to mirror.

## 1. `FinancialLedgerEntry` (append-only, immutable)
`Payment.Domain/Entities/Reporting/FinancialLedgerEntryEntity.cs`: `EntryCode` (unique), `AccountLine` (enum — every
§15+§19.17 line below), `EntryNature` (`Revenue / Expense / Liability / Receivable / Memo`), `Amount` (always **positive**;
nature carries the sign in reports — do not store negatives), `CurrencyCode`, `SourceType` (`AcceptanceSnapshot / Refund /
Chargeback / PremiumPurchase / Subscription`), `SourceRef` (the source id), `TransactionId?`, `ProviderProfileId?`,
`CustomerProfileId?`, `OccurredAtUtc`, `PostedAtUtc`. Private setters, insert-only, validating `Create` factory.
- **Idempotency:** unique `(SourceType, SourceRef, AccountLine)` — re-posting the same source line is a no-op (reversal/
  correction is a NEW entry, never a mutation). Reuse the ProcessedGatewayEvent/CapturedAt style guard.

## 2. `LedgerAccountLine` enum — §15 + §19.17 (exact, no merging)
Revenue: `ProviderCommissionRevenue, CustomerPlatformFeeNetRevenue, SubscriptionRevenue, ProviderPlanRevenue,
CustomerPlanRevenue, ProviderAddOnRevenue, PremiumProductRevenue`.
Liability (NOT revenue): `CustomerPlatformFeeVatLiability`.
Expense: `PaymentProcessingExpense, GatewayOtherExpense, RefundProcessingExpense, ChargebackExpense,
PlatformFundedCustomerDiscountExpense`.
Discount/benefit (kept SEPARATE per §19.17 — never one `DiscountAmount`): `ProviderFundedCustomerDiscount` (memo — **NOT an
Inktavia expense**), `ProviderCommissionBenefitCost`.
Recovery/advance: `ProviderRecoveryReceivable, PlatformAdvancedRefundAmount`.
Contribution/settlement (computed/memo): `PlatformGrossShare, PlatformSettlementNetAmount, CustomerSideContribution,
ProviderSideContribution, TotalTransactionContribution, NetMarketplaceContribution`.
Expected/actual expense (§19.17): `ExpectedPaymentProcessingExpense, ActualPaymentProcessingExpense, RefundRiskReserve,
ActualRefundExpense`.

## 3. Posting service (derived from immutable sources) — `FinancialLedgerPostingService`
Post the correct lines at each event, reading amounts **from the source snapshot/allocation (never recompute rates)**:
- **Acceptance (snapshot created):** `ProviderCommissionRevenue` = snapshot.CommissionAmount; `CustomerPlatformFeeNetRevenue`
  = fee net; `CustomerPlatformFeeVatLiability` = fee vat (Liability, excluded from revenue); `PlatformFundedCustomerDiscountExpense`
  = snapshot.TotalPlatformFundedDiscount (Inktavia campaign cost); `ProviderFundedCustomerDiscount` = TotalProviderFundedDiscount
  (memo, NOT expense); `ProviderCommissionBenefitCost`; `PlatformGrossShare`; the three contributions + `NetMarketplaceContribution`
  memo. `ExpectedPaymentProcessingExpense`/`RefundRiskReserve` from the profit-protection context if recorded.
- **Refund (P10 allocation):** reverse the affected revenue lines (new negative-nature entries as reversals) +
  `RefundProcessingExpense` (gateway refund expense) + `GatewayOtherExpense` + `ProviderRecoveryReceivable` +
  `PlatformAdvancedRefundAmount` + `ActualRefundExpense` from the 9-amount allocation.
- **Chargeback:** `ChargebackExpense` + recovery lines (mirrors refund recovery).
- **Premium paid:** `PremiumProductRevenue` = purchase.UnitPriceSnapshot; refund → reversal.
- **Subscription charge:** `SubscriptionRevenue` + the §19.17 split `ProviderPlanRevenue` / `CustomerPlanRevenue` /
  `ProviderAddOnRevenue`.
- **Rules (§19.17, binding):** discounts NEVER merged into a single `DiscountAmount`; **provider-funded customer discount is
  NOT reported as an Inktavia expense** (memo line); **platform-funded discount IS** an explicit Inktavia campaign/membership
  cost; **VAT liability is not revenue**.

## 4. Backfill (existing sources → ledger, idempotent)
A one-off backfill posts ledger entries for existing snapshots / refunds / chargebacks / premium purchases / subscriptions
(idempotent via the unique constraint) so historical data reports correctly. Append-only; safe to re-run.

## 5. Period report queries (read models)
- `GetFinancialSummaryReport(from, to, currency)` → revenue total (excl. VAT liability), expense total, and
  **`NetMarketplaceContribution = Σ revenue − Σ (payment + refund + chargeback + discount expenses)`** (§15 formula), plus
  the per-line breakdown. VAT liability + provider-funded discount shown separately (not in Inktavia P&L).
- `GetLedgerEntries(filter: account line / source / provider / period)` (paged) — audit/drill-down.
- Optionally per-provider contribution + `PlatformSettlementNetAmount` (§13.10) rollup. Reuse the
  `GetFinanceInvoiceStatementReport` read pattern; admin-scoped (`[Authorize(Roles=Admin)]`).

## 6. Persistence / migration (append-only, §19.18)
New table `financial_ledger_entries` (numeric(18,4), enum int, unique `(SourceType, SourceRef, AccountLine)` + unique
`EntryCode`, indexes on `(AccountLine, OccurredAtUtc)` + `(ProviderProfileId, OccurredAtUtc)` + `CurrencyCode`). DbSet, EF
config, DI, repository (append + paged/aggregate reads). Backfill migration/seed step (idempotent). `PaymentErrorCode`
additions if needed (e.g. `LedgerEntryDuplicate` — but the unique constraint + no-op is preferred over throwing).

## 7. Tests
- **Posting per event:** acceptance snapshot → the exact revenue/liability/discount/benefit/contribution lines with amounts
  == snapshot fields; refund → reversal + refund/gateway/recovery/advance lines == allocation; chargeback → ChargebackExpense
  + recovery; premium paid → PremiumProductRevenue; subscription → SubscriptionRevenue (+ §19.17 split).
- **§19.17 rules:** discounts not merged; provider-funded discount is a memo NOT an expense; platform-funded discount is an
  expense; VAT liability excluded from revenue.
- **Idempotency:** re-posting the same source (duplicate webhook/backfill re-run) → no duplicate entries (unique constraint).
- **NetMarketplaceContribution:** a synthetic period with revenues + expenses → the formula holds; VAT liability and
  provider-funded discount excluded from the Inktavia contribution.
- **Reconciliation:** Σ ledger per source == the source's own amounts (snapshot/allocation/purchase) to the cent.
- **Backfill:** existing snapshots/refunds/premium/subscriptions → ledger populated once; re-run no duplicates.
- **Rounding:** `MoneyMath`.

## 8. Acceptance criteria
- Append-only `FinancialLedgerEntry` with all §15+§19.17 account lines; posted from immutable sources (derived, not
  recomputed); §19.17 rules enforced (no merged discount, provider-funded ≠ expense, platform-funded = campaign cost, VAT
  ≠ revenue); idempotent posting + backfill; period `NetMarketplaceContribution` per the §15 formula + per-line breakdown +
  drill-down, admin-scoped. Reporting is additive — no economics/refund/premium logic changed. Append-only migration; build
  clean; existing paths green. No CargoDry.

## 9. Verify — run and PASTE output
1. `dotnet build` Payment: 0 errors.
2. Rebuild + `docker compose up -d --force-recreate payment-api`; clean boot; migration + backfill applied.
3. DB: `financial_ledger_entries` present; `SELECT "AccountLine", count(*), sum("Amount") FROM payment.financial_ledger_entries GROUP BY 1;`
   (backfilled lines).
4. Tests green (paste): posting per event + amounts == source, §19.17 rules, idempotency, NetMarketplaceContribution
   formula, reconciliation to sources, backfill re-run no-dup, rounding.
5. Smoke: run `GetFinancialSummaryReport` over a seeded period → revenue/expense/contribution consistent with the snapshots
   + refunds + premium in that window; VAT liability + provider-funded discount shown separately.

## 10. Report
`REPORT_BACKEND.md` ("BE-P12"): append-only financial ledger (§15+§19.17 lines) + posting service (acceptance/refund/
chargeback/premium/subscription, derived from immutable sources) + §19.17 discount/VAT rules + idempotent backfill + period
`GetFinancialSummaryReport` (NetMarketplaceContribution) + drill-down. Note: FE admin reporting dashboard = FE_ADMIN;
per-provider settlement rollup extensible; live figures verified once real payments flow (P9 gate). **This completes the
Payment backend core P1–P12.** Next: live P9 sandbox gate (keys) + FE (admin/provider) reporting surfaces + remaining
module roadmaps (RefData R, Identity I2–I3, Notification N, SR S9–S13). Do not touch CargoDry.
