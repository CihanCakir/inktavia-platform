# FE_PROVIDER Wave B — finance transparency (extend the live provider finance screens; FE-only)

> **Repo:** `inktavia-marine-provider-web` (FE only). After the admin rule-CRUD wave (P2/P3/P5/P6/P7), this pivots to
> user-visible **provider** value: show the provider, in their existing Finance screens, exactly where the money goes.
> **The data is already exposed** — BFF-Wave 3/4 enriched the provider module DTOs (pass-through), so this is a pure
> **display extension**: type the fields, render them additively, don't break the design. **Server owns every total —
> the FE only reads and formats, never computes.**
>
> **Do NOT touch** the admin panel, CargoDry, or the backend/BFF (the fields already ship). Extend the existing Finance
> feature additively (nautical tokens, TanStack Query, `normalizeEnvelope`, the existing `DetailDrawer`). Null-safe:
> legacy rows without the new fields must render cleanly (older transactions have no snapshot).
>
> **Scope = the three transparency pieces (P8/S8, P4, P10). P11 offer-boost is a PURCHASE flow on the offers surface,
> not finance display — it gets its own follow-up kickoff (BFF-Wave 4 already added the provider boost command +
> boost-status). Do NOT build boost here.**

## 0. Ground truth (confirmed — align to these exact BFF/DTO field names)
Existing FE: `src/features/finance/` — `financeApi.ts`, `useFinance.ts`, `FinancePage.tsx` (tabbed), `PayoutsPage.tsx`,
`components/DetailDrawer.tsx`, `components/charts.tsx`, `lib/csv.ts`. The new fields are **NOT yet in the FE types** →
add them.

Provider BFF returns these (module Abstraction DTOs, pass-through):
- **P8/S8 — `ProviderTransactionDto`** → `EconomicsSnapshotId?` + `EconomicsBreakdown?`
  (`ProviderTransactionEconomicsBreakdownDto`): `ServiceAmount`, `CommissionBaseAmount`, `ProviderNetAmount`,
  `PlatformFeeNetAmount`, `PlatformFeeVatAmount`, `PlatformFeeGrossAmount`, `CustomerTotalAmount`, `PlatformGrossShare`
  (+ any commission/discount-funding fields present — read them all; treat missing as null). Legacy tx → breakdown null.
- **P10 — `ProviderTransactionDto`** → `DisputedAt?` + `RefundSummary?` (`ProviderTransactionRefundSummaryDto`:
  `PlatformAdvancedRefundAmount`, `ProviderRecoveryAmount`, `RemainingProviderNegativeBalance`, + the refund
  cause/release fields present). **`ProviderPayoutDto`** → `NegativeBalance?` (`ProviderBalanceSummaryDto`),
  `NegativeBalanceLimit`, `IsOverLimit`.
- **P4 — `ProviderSubscriptionDto`** → `ActivePrice?` (`ProviderPlanActivePriceDto`, has a `PriceType` label
  **"Launch" | "List"** + amount), `HasUpcomingPriceChange`, `UpcomingPriceAmount?`, `UpcomingPriceChangeAt?`.

## Part A — P8/S8 economics breakdown (headline: "where the money goes")
In the transaction/settlement **`DetailDrawer`**, when `EconomicsBreakdown` is present, add a "Kazanç kırılımı" section
that walks the money one line at a time (all read-only, formatted TRY):
`Müşteri öder (CustomerTotalAmount)` → `Hizmet tutarı (ServiceAmount)` → `Platform ücreti (PlatformFeeGrossAmount, with
net/VAT sub-line)` → `Komisyon matrahı (CommissionBaseAmount)` → `Komisyon (the commission amount field)` → **`Sana geçen
net (ProviderNetAmount)`** as the gold-emphasised takeaway. Show `PlatformGrossShare` and any discount-funding line if
present. This mirrors the offer-builder's "Sen ne alırsın" panel but for a settled transaction. Legacy tx (no snapshot)
→ show the existing summary only, no breakdown section (null-safe).

## Part B — P4 subscription launch-vs-list + upcoming change
On the **Subscription** tab (FinancePage), surface `ActivePrice.PriceType`: a **"Lansman fiyatı"** badge when "Launch"
(with a short "kampanya dönemi" note) vs the standing **"Liste fiyatı"** when "List", next to the amount. When
`HasUpcomingPriceChange`, show an inline notice: "Yenilemede fiyat değişecek: `UpcomingPriceAmount` — `UpcomingPriceChangeAt`"
(date-formatted). Purely informational; no actions. Null-safe when the fields are absent.

## Part C — P10 negative balance + disputed / refund transparency
- **Payouts** (`PayoutsPage`): when `NegativeBalance` is present, show a balance strip — current negative balance,
  `NegativeBalanceLimit`, and an `IsOverLimit` warning state (nautical danger token) explaining that payouts are offset
  against it. Read-only.
- **Transactions**: when `DisputedAt` is set, show an **"İtirazlı"** status chip on the row + in the DetailDrawer. When
  `RefundSummary` is present, add a refund breakdown to the DetailDrawer: what the platform advanced
  (`PlatformAdvancedRefundAmount`), what's being recovered from the provider (`ProviderRecoveryAmount`), and the
  remaining negative balance (`RemainingProviderNegativeBalance`) — framed transparently so the provider understands a
  clawback/offset. Null-safe.

## Don't-break / QA
- Additive only: existing Finance tabs/rows/DetailDrawer/charts/CSV keep working; new sections appear only when the
  fields exist. Nautical tokens (navy/gold/danger/warning); no new design language.
- Server-owns-totals: the FE formats the provided decimals; it never recomputes a total or a share.
- Extend `financeApi.ts` types + `useFinance.ts` selectors additively; envelope-tolerant via `normalizeEnvelope`
  (unknown/renamed field → null, never throw). i18n (tr+en) for all new labels — full parity.
- `npm run typecheck` clean; existing finance flows unregressed; admin/CargoDry untouched; no backend/BFF changes.

## Verification (on-screen)
Log into provider-web as an active provider. **Part A:** open a settled transaction with an economics snapshot → the
breakdown walks CustomerTotal → ServiceAmount → PlatformFee(gross/net/VAT) → CommissionBase → Commission → ProviderNet
(gold); a legacy transaction shows no breakdown, no error. **Part B:** the Subscription tab shows the Launch/List badge
and, if applicable, the upcoming-price notice. **Part C:** payouts show the negative-balance strip (+ over-limit
warning when set); a disputed transaction shows the "İtirazlı" chip + refund breakdown in the drawer. typecheck clean.

## Report
`inktavia-marine-provider-web/docs/.../REPORT_FE_PROVIDER_WAVE_B.md` (or `addesso-project/docs/V1.0.1/Payment/`):
fields typed + where each renders, the null-safe/legacy handling, on-screen transcript. Flag P11 offer-boost as the next
provider piece (its own purchase-flow kickoff). After Wave B, the remaining admin options are P10 refund/chargeback
queue + I1 sub-merchant KYC, and the P12 reporting dashboard.
