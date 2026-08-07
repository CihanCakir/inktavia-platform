# Provider QA — Finance (P-QA3)

## Static findings
Implemented (`finance` api+hooks+2 pages: `/app/finance`, `/app/finance/payouts`) — the PAY-0..5 provider finance surface
(6 tabs: settlements / payouts / transactions / invoices / subscription / profile). Needs a **correctness live-QA** against
the real economics/ledger snapshots.

## Live walkthrough checklist (localhost:3002/app/finance)
- [ ] Each of the 6 tabs loads (loading/empty/error), figures match the underlying snapshots (ServiceAmount / PlatformFee /
      CommissionBase / provider net; funding split; commission benefit).
- [ ] Payouts page: pending/held/completed states; negative-balance / clawback surfaced transparently (P10).
- [ ] Subscription tab: launch vs list price; upcoming price-change reminder (N1) reflected.
- [ ] Invoices render + open; transaction "disputed" state shows where relevant (S13).
- [ ] No stale mock/hardcoded figures; currency formatting correct (TRY).

## Fix candidates
Runtime/visual/number issues found → `FIX_*` doc here.
