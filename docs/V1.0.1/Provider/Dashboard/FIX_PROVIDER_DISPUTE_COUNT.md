# FIX — provider-facing dispute count/list (unblocks the dashboard "disputes" attention row)

> **Repos:** ServiceRequest module + MarineProvider BFF + `inktavia-marine-provider-web`. P-QA1 intentionally omitted the
> dashboard "disputes needing input" attention row because **there is no provider-facing dispute count/list** today
> (`GetDisputeCaseDetail` supports a `providerUserId`, but there's no "my disputes" list for a provider). Add a small
> provider-scoped surface. Additive, no economics change. **Do not commit** until reviewed.

## What's known
- Disputes are owner/admin-oriented: `GetAdminDisputeList` (admin), `GetDisputeCaseDetail(providerUserId?)` (a party can
  view one case). **No provider "my disputes" list/count** endpoint.
- The provider is a party to disputes on their assigned service requests (S13). The dashboard attention strip + a disputes
  view need the provider's open/actionable disputes.

## Change
1. **SR module:** add a provider-scoped query — `GetProviderDisputes(providerUserId, status?, paging)` returning the
   disputes where the provider is a party (via their assignments/offers), + a lightweight **count** of open/actionable ones.
   Reuse the dispute repository; identity is the provider (server-side), never from the body. Cost-free (no S5 cost fields).
2. **MarineProvider BFF:** passthrough `GetProviderDisputesBff` (list) + expose the open count (either a dedicated
   count endpoint or derived from the list) — typed, envelope-correct, provider identity via BffAssertion.
3. **provider-web:**
   - **Dashboard:** add the previously-omitted **"disputes needing input"** attention row, fed by the open count, linking to
     the disputes view; hide the row at 0 (consistent with the other attention rows).
   - **(Optional, if in scope) a provider disputes list/detail** view so the link has a destination — or route it to the
     relevant service-request/job detail where the dispute surfaces, if a full disputes screen is deferred.

## Verify (on screen)
- [ ] The dashboard shows a real "disputes needing input" count when the provider has open disputes (hidden at 0).
- [ ] The row links to a working destination (disputes view or the SR/job carrying the dispute).
- [ ] Count is provider-scoped (only the provider's disputes), identity from token; no cost fields exposed.
- [ ] tsc + eslint clean; tr+en; no console errors.

## Report
`docs/V1.0.1/Provider/Dashboard/REPORT_PROVIDER_DISPUTE_COUNT.md`: the SR provider-dispute query + BFF passthrough, the
dashboard attention row wiring (+ destination), and verification. This closes the last P-QA1 attention-strip gap.
