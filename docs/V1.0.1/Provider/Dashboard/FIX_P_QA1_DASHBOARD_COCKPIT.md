# FIX P-QA1 — dashboard: wire the placeholder metrics + make it a real cockpit

> **Repo:** `inktavia-marine-provider-web` — FE only, additive. Turns the dashboard from 5 dead placeholder tiles into a
> live provider cockpit, reusing the feature queries that already exist. `MetricCard` already supports
> `value`/`loading`/`error`/`to`/`planned` — so wiring = pass real data + drop `planned`. tr+en. Report per the QA cadence.

## Findings (static)
`src/features/dashboard/pages/DashboardPage.tsx` renders 6 tiles; only **jobs** is wired (`useProviderJobsQuery`). Five are
`planned` placeholders with no data despite live backends: `openOffers`, `serviceRequests`, `unreadMessages`,
`financeSnapshot`, `pendingRenewals`. `MetricCard` deliberately shows no fake number when `planned` — so the fix is to feed
real values (or a clean empty/zero) and remove `planned`.

## Available data sources (reuse — don't add backend unless nothing exists)
- **Open offers** — the offers feature's list query (as `OffersPage` uses); count the provider's active/pending offers.
- **Active service requests** — `useProviderJobsQuery` (assigned/in-progress) and/or the SR feature api.
- **Unread messages** — `messages/hooks/useMessages` (unread count; the shell bell already tracks unread).
- **Finance snapshot** — `finance/hooks/useFinance` (pending payout / available balance / this-period earnings from the
  PAY-0..5 summary).
- **Pending renewals** — `cargodry/hooks/useCargoDry` (upcoming renewals).
> Prefer an existing summary/count; if a feature exposes only a list, derive the count from it; **only** add a lightweight
> BFF count/summary endpoint if neither exists (flag it in the report — don't fabricate a number, `MetricCard` forbids that).

## Change
1. **Wire the 5 tiles:** replace each `planned` `MetricCard` with a data-fed one — `value` from the reused query,
   `loading`/`error` from its state, `to` linking to the module. Remove the `planned` prop once wired. A genuine zero shows
   as `0` (real), not a placeholder.
2. **Cockpit UX (additive, keep it clean):**
   - An **"attention" strip**: offers awaiting your response, jobs due today, disputes needing input, negative/low finance
     balance — each links to the item. Derive from the same queries; hide a row when its count is 0.
   - A **recent activity** feed (recent jobs already exist; extend with recent offers/messages if cheap) — reuse, don't
     over-build.
   - Every widget: loading skeleton on first paint, clean empty state, error state on failure. No tile that never resolves.
3. Keep the existing jobs list section.

## Verify (on screen — localhost:3002/app/dashboard)
- [ ] All 5 metrics show real numbers (or a clean zero/empty), no "planned" placeholder.
- [ ] Each tile navigates to its module; attention strip reflects real pending work and links correctly.
- [ ] Loading skeletons first paint; a failing endpoint shows an error state, not a blank/placeholder.
- [ ] No console errors; tr+en labels complete.

## Report
`docs/V1.0.1/Provider/Dashboard/REPORT_P_QA1.md`: which query fed each metric (+ any new count endpoint added and why), the
cockpit additions, and the on-screen verification.
