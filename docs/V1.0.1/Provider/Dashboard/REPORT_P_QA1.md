# REPORT — P-QA1: wire the placeholder dashboard metrics + make it a real cockpit

> Executes `FIX_P_QA1_DASHBOARD_COCKPIT.md`. `inktavia-marine-provider-web`, **FE only, additive.** Turns 5 dead
> `planned` tiles into live metrics by reusing existing feature queries, and adds a cockpit "attention" strip.
> `MetricCard` (value/loading/error/to/planned) unchanged. Verified live on `localhost:3002/app/dashboard`, tr+en.

---

## Metrics — each `planned` tile now fed by a reused query

| Tile | Source query (reused) | Value | Link |
|------|-----------------------|-------|------|
| `activeJobs` (was already live) | `useProviderJobsQuery({pageSize:50})` | count of `Assigned/Accepted/InProgress/Scheduled` | Jobs |
| **`openOffers`** | `offersApi.getMine({pageSize:100})` (shared cache key with OffersPage) | count of `Submitted/UnderReview` (live offers); hint = total | Offers |
| **`serviceRequests`** | `useDiscoverySummary(DEFAULT_FILTERS)` → `openCount` | open requests available; hint = `publishedTodayCount` | Service Requests |
| **`unreadMessages`** | `useConversations()` | Σ `unreadCount` across conversations; hint = conversation count | Messages |
| **`financeSnapshot`** | `usePayoutSummary()` → `pendingPayout` | formatted currency (pending payout) | Finance |
| **`pendingRenewals`** | `useCargoDryRenewals()` | count of upcoming renewal candidates | CargoDry renewals |

`planned` was removed from all five. Each tile passes `loading`/`error` from its query state (`isPending`, and
`isError || !result.ok`), so a genuine `0` renders as `0`, loading shows the skeleton, and a failure shows
`cardError` — never a fabricated number. **No new backend endpoint was needed** — every metric came from an
existing query/summary (per the "reuse, don't add backend" rule).

## Cockpit — "attention" strip (additive)

A strip above the metrics surfaces only real pending work; a row is hidden when its count is 0, and the whole strip
is hidden when empty (a slim skeleton shows on first paint):

- **Offers awaiting a decision** — active offers count → Offers.
- **Jobs scheduled today** — active jobs whose `scheduledStartDate` is today → Jobs.
- **Negative balance to clear** — `selectNegativeBalance(usePayouts())` (`isOverLimit || negativeAmount > 0`),
  danger-toned, amount formatted → Finance.

**Disputes row — intentionally omitted (not fabricated).** There is no provider-side disputes count/API or a
`Disputed` job status in this repo (grep of `features/**` found no disputes api/hook and the provider job statuses
are only `Assigned/InProgress/Scheduled/Completed`). `MetricCard`/the cockpit forbid inventing a figure, so the
disputes row is left out until a real source exists (a follow-up could add a lightweight BFF dispute-count).

**Recent activity** — the existing "Recent jobs" list is kept (the recent-activity feed), now sliced to the top 5
of a wider page; loading skeleton / empty state / error state all retained. No over-building.

## Quality gates

- `tsc --noEmit` → **0 errors**; `eslint src/features/dashboard/pages/DashboardPage.tsx` → **clean**.
- i18n `dashboard` namespace: added `openOffersHint`, `serviceRequestsHint`, `unreadMessagesHint`,
  `financeSnapshotHint`, `pendingRenewalsHint`, and `attention.{title,offers,jobsToday,balance}` — **en/tr parity
  30/30** (nested keys included).

## On-screen verification (localhost:3002/app/dashboard, logged in as PROVIDER 2 AS)

- ✅ **All 5 metrics real** (no `planned` placeholder): Active jobs **1**, Open offers **0** ("4 total"),
  Service requests **48** ("0 published today"), Unread messages **0** ("across 1 conversation(s)"),
  Finance snapshot **$0.00** ("pending payout awaiting disbursement"), Pending renewals **1**. Real zeros show as `0`.
- ✅ **Attention strip reflects real work**: "Negative balance to clear: TRY 10,560.00" (danger, links to Finance).
  The offers/jobs-today rows are correctly hidden (both counts are 0).
- ✅ **Every tile + attention row is a real link** to its module — verified via the DOM (`/app/offers`,
  `/app/service-requests`, `/app/messages`, `/app/finance`, `/app/cargodry/renewals`, `/app/jobs`).
- ✅ **Recent jobs list preserved** (Service request #9011 → `/app/jobs/91001`, Assigned).
- ✅ **No console errors** (checked after a reload with error-only console tracking).
- ✅ **tr + en both complete** — verified by toggling the language switcher: Turkish ("DİKKATİNİZİ BEKLİYOR",
  "Kapatılacak negatif bakiye…", "Aktif İşler", "Servis Talepleri", "Bekleyen Yenilemeler", …) and English render
  fully with no raw keys or missing labels. Active nav item (Dashboard) highlighted correctly.
- Loading/error paths are wired on every widget (MetricCard `loading`/`error`, attention-strip + jobs skeletons).
  First paint resolved to real data quickly; a forced single-endpoint failure was not exercised live (would need
  backend manipulation) but every tile carries the `error` → `cardError` path.

**Observation (not in scope):** the finance-snapshot value renders with `$` while the negative-balance row renders
`TRY` — they come from two different real backend sources (`payoutSummary.currencyCode` vs the balance ledger's
`currencyCode`); both values are truthful. Reconciling the provider's display currency is a finance-module concern,
not this dashboard fix.

## Scope

Changed: `src/features/dashboard/pages/DashboardPage.tsx` + `en/tr` `dashboard.json`. No backend, no `MetricCard`
change, no new endpoint. **Not committed.**
