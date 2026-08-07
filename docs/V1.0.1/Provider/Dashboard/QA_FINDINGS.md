# Provider QA — Dashboard (P-QA1)

## Static findings
`src/features/dashboard/pages/DashboardPage.tsx` renders **6 metric cards, 5 of them `planned` placeholders with no data**;
only **jobs** is wired (`useProviderJobsQuery` + a recent-jobs list). Placeholder cards: `openOffers`, `serviceRequests`,
`unreadMessages`, `financeSnapshot`, `pendingRenewals` — all have live backends now, so they should carry real numbers.

## Planned work — turn the dashboard into a real cockpit
1. **Wire the 5 placeholder metrics** to their live sources:
   - **Open offers** — count of the provider's active/pending offers (offers api).
   - **Active service requests** — assigned/in-progress count (jobs/SR api).
   - **Unread messages** — messaging unread count (messages api, already realtime).
   - **Finance snapshot** — pending payout / available balance / this-period earnings (finance api, PAY-0..5 snapshots).
   - **Pending renewals** — upcoming renewals count (renewals/cargodry api).
2. **UX upgrades:** each metric card links to its module; add an **"attention" strip** (offers awaiting your response,
   jobs due today, disputes needing input, low finance/negative balance) and a **recent-activity** feed; loading/empty/error
   states on every widget (no placeholder that never resolves).
3. Remove the `planned` prop from `MetricCard` usages once wired.

## Live walkthrough checklist (localhost:3002/app/dashboard)
- [ ] Every metric shows a real number (or a clean empty/zero state), not a "planned" placeholder.
- [ ] Each card navigates to its module.
- [ ] Attention items reflect real pending work; recent activity is real.
- [ ] No console errors; loading skeletons on first paint; error state if an endpoint fails.
