# Provider QA — Performance (P-QA7)

## Static findings
`PerformancePage` renders the **`PlannedPage` "Coming Soon" stub** — genuinely unbuilt (0 query hooks). Nav item is in the
**finance** group and marked `planned`. The backend has provider performance data (`AdminProfilePerformance` +
finance/ledger), so it CAN be built.

## Decision needed
Build now vs hide-from-nav-until-built. Recommendation: **build** (the data exists) — a provider-facing performance/earnings
view is valuable for go-live; if descoped, **remove the nav entry** so it doesn't dead-end on "Coming Soon".

## If building — scope
Provider KPIs: completed jobs, ratings, response time, acceptance rate, earnings trend (from finance/ledger +
profile-performance). Add `features/performance/hooks` + wire the page (loading/empty/error).

## Live walkthrough checklist
- [ ] Page shows real KPIs (not the Construction stub), or the nav entry is removed.
