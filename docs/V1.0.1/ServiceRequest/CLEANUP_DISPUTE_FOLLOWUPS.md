# CLEANUP — dispute follow-ups: transaction-less refund 500 + dispute-list all-status

> **Repo:** `addesso-project` — ServiceRequest + Payment modules + AdminPanel BFF + admin-web. Two small follow-ups found
> during S13 FE verification. Both **additive / behaviour-correcting**, no economics change. **Do not commit** (folds into
> the uncommitted dispute vertical for a single review).

## Fix 1 — partial refund on a transaction-less dispute returns a raw 500 (diagnostic-first)
**Symptom:** resolving a dispute with a partial (or full) refund outcome, when the dispute's SR has **no Payment
transaction**, returns a raw **500** instead of a clean business error.
**Known:** the Payment `ResolveDisputeOutcomeCommandHandler` **already handles `txHead is null` gracefully** — it returns
`{ Applied = false, Message = "No transaction for this service request." }` (a 200-level result, not a throw). So the 500
is **not** that path. **Diagnose before fixing** — find the actual throw. Likely candidates:
- the **SR-side resolve handler** (`ResolveServiceRequestDispute…`) mishandling an `Applied = false` remote-call result for
  a refund-requiring outcome (treating "no money moved" as a hard failure), or
- the **refundable / payment-state lookup** used to validate `amount ≤ refundable` throwing when the SR has no
  transaction (a null/absent transaction → unguarded access), or
- an **envelope/remote-call** mismatch surfacing the graceful `Applied=false` as a 500.
**Fix:** once located, convert the failure into a **clean `AizenBusinessException`** with a specific code (SR or Payment
range, e.g. `DisputeRefundNoTransaction`) and message, so the admin FE shows a readable business error
("Bu talebin ödemesi olmadığı için iade yapılamaz — notlarla çözün") instead of a 500. A **notes-only or provider-release**
resolve on a transaction-less dispute must still succeed (nothing to move). Keep the Payment handler's graceful
`Applied=false` — the SR/BFF layer should translate it (or the pre-validation should reject early with the business code).
**Test:** resolving a partial/full refund on a transaction-less dispute → clean business error (no 500); notes-only /
provider-release on the same dispute → still succeeds.

## Fix 2 — dispute list only returns Open disputes
**Symptom:** the admin dispute list shows only **Open** disputes; Resolved/Closed/UnderReview/etc. never appear.
**Cause:** `GetAdminDisputeListQueryHandler` calls `_repository.GetAllOpenAsync(...)`; the repo only exposes
`GetAllOpenAsync`.
**Fix (additive, back-compat):**
- Add a repo method `GetAllAsync(ServiceRequestDisputeStatus? status, int skip, int take, ct)` (keep `GetAllOpenAsync` or
  reimplement it in terms of the new one) returning disputes **of all statuses**, optionally filtered by a status.
- Extend `GetAdminDisputeListQuery` with an **optional `status` filter** (default **null = all**); the handler uses
  `GetAllAsync`. Ensure the total count reflects the filter.
- Thread the optional `status` through the **AdminPanel BFF** `GetDisputeListBff` query (additive) and the **admin-web**
  dispute list: default shows **all** statuses with a visible **status badge/column**, plus an optional **status filter**
  control. tr + en for any new label.
**Test:** the list returns disputes across statuses (Open, UnderReview, Resolved, Closed…); the optional status filter
narrows correctly; existing default behaviour still lists (now all, not only Open).

## Don't-break / QA
- Additive: a business-error code + a repo method + an optional query/BFF/FE filter. No economics, no refund-math change,
  no route change (add a query param only). Migrations: none. Builds clean; admin-web tsc + eslint clean; tr+en.
- Fix 1 is **diagnostic-first** — reproduce the 500, locate the real throw, then convert; don't guess-patch.

## Report
`docs/V1.0.1/ServiceRequest/REPORT_CLEANUP_DISPUTE.md`: the located 500 source + the clean business-error code, the
all-status dispute list (repo/query/BFF/FE + filter), and the tests. **Do NOT commit.**
