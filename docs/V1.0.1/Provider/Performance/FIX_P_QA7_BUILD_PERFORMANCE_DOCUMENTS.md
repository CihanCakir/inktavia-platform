# FIX P-QA7 — build the Performance + Documents stubs (backends exist)

> **Repos:** `inktavia-marine-provider-web` + MarineProvider BFF (`addesso-project/Bff/src/MarineProvider`). Both pages
> currently render the `PlannedPage` "Coming Soon" stub, but their **backends exist** → build them (no reason to ship a
> nav item that dead-ends). Additive, tr+en. **Do not commit** until reviewed. (Cross-dir: `Documents/` mirrors this.)

## Backends confirmed
- **Performance:** `Modules/Profile` has a full performance subsystem — `GetPerformanceSnapshotByProfile` (+ByTier),
  Performance entities/DTOs/consumers. So a per-provider **performance snapshot** exists.
- **Documents:** MarineProvider BFF already has `Onboarding/Command/AttachOnboardingDocument`, `DeleteOnboardingDocument`,
  `GetDocumentAccessUrl` — the provider's KYC/onboarding docs. A **list** query is the likely gap.

## Performance page
- **BFF:** add a `GetProviderPerformanceBff` query → `IProfileRemoteCall.GetPerformanceSnapshotByProfile` for the
  authenticated provider's profile (identity from token, BffAssertion). Add the `IProfileRemoteCall` to the MarineProvider
  BFF if not present (mirror the existing remote-call pattern). Typed, envelope-correct.
- **FE:** replace `PerformancePage`'s `PlannedPage` with a real KPI view from the snapshot — completed jobs, rating,
  response time, acceptance/completion rate, and (reuse) an earnings trend if the snapshot carries it. Add
  `features/performance/hooks`; loading/empty/error states; tr+en. Keep it read-only.

## Documents page
- **BFF:** ensure a **list** surface exists — a `GetOnboardingDocumentsBff` (list the provider's documents with type/status/
  uploaded-at); reuse the existing attach/delete/access-url commands for actions. If the module lacks a list query, add a
  small one (provider-scoped).
- **FE:** replace `DocumentsPage`'s `PlannedPage` with a real list — the provider's compliance/KYC/onboarding documents
  (type, status badge, uploaded date), with **download** (via `GetDocumentAccessUrl` presigned) and **upload/replace**
  (AttachOnboardingDocument) + delete; loading/empty/error; tr+en. Reuse the vessel-document upload pattern.

## Decision fallback
If either backend turns out thinner than expected (e.g. no meaningful performance snapshot data yet), **hide that nav item**
until it's real rather than shipping the stub — but prefer building, since the data sources exist.

## Verify (on screen)
- [ ] `/app/performance` shows real KPIs (not the Construction stub); loading/empty/error clean.
- [ ] `/app/documents` lists the provider's real documents; download works (presigned), upload/replace/delete work.
- [ ] Both nav items lose the "YAKINDA" badge (reconcile `navigation.ts` status → `implemented`).
- [ ] tsc + eslint clean; tr+en parity; no console errors.

## Report
`docs/V1.0.1/Provider/Performance/REPORT_P_QA7.md` (+ note Documents): the BFF passthroughs added, the two pages built,
the nav-flag reconcile, and the on-screen verification.
