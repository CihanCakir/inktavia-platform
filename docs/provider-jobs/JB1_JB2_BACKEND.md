# JB-1 + JB-2 — Backend: Jobs enrichment + summary counts

Unlocks the Jobs dashboard's table (İş/Tekne columns) and its KPI board + donut. Run **JB-1**, then **JB-2**.
Provider-scoped; keeps the module-boundary rule (modules never call each other — the BFF orchestrates cross-module).

## Key insight (why this is cheap)
The **assignment and the ServiceRequest are in the SAME module**. So the job's `Title`, `RequestCode`, and `VesselId`
come from an **in-module join** — no cross-module call. Only the **vessel NAME** lives in the Vessel module, so it is
the single thing the BFF bulk-enriches (exactly like the detail page's `EnrichVesselAsync`).

## Verified in source (2026-07-16)
- Module `GetProviderJobsQueryHandler` → `_assignmentRepository.GetByProviderProfileIdAsync(providerProfileId, skip,
  take)` → projects `ProviderJobItemDto { AssignmentId, ServiceRequestId, ServiceRequestOfferId, Status, dates,
  ProviderNotes }`. No title/vessel/code.
- Assignment entity has `ServiceRequestId` (and a `ServiceRequest` navigation). The SR carries `Title`, `RequestCode`,
  `VesselId`.
- BFF `GetProviderJobsQueryHandler` calls `_serviceRequest.GetProviderJobs(...)` and does **no** vessel enrichment
  today (unlike the detail handler, which uses `IProviderVesselRemoteCall.GetSummaries`).
- Assignment repo has **no** status-count method (only paged fetch).

---

## Phase JB-1 — Enrich jobs with Title + Vessel + Code

### 1. Module — add SR fields via in-module join
- Ensure the jobs query includes the ServiceRequest for each assignment (`.Include(a => a.ServiceRequest)` or a join
  in the projection — same DbContext, no cross-module call, no N+1: one query).
- Add to `ProviderJobItemDto`: `string Title` (`a.ServiceRequest.Title`), `string RequestCode`
  (`a.ServiceRequest.RequestCode`), `long VesselId` (`a.ServiceRequest.VesselId`).
- The design's human code "OP-4492" is cosmetic — **reuse `RequestCode`** (e.g. `SR-…`). Do not add a new schema
  column unless the product owner specifically wants an assignment code.

### 2. BFF — bulk-enrich Vessel name (mirror the detail handler)
- Inject `IProviderVesselRemoteCall` into the BFF `GetProviderJobsQueryHandler`.
- Collect the distinct `VesselId`s from the page, one **bulk** call `GetSummaries(ids)` →
  `VesselSummaryDto { VesselId, Name, … }`, map `VesselName` back onto each job by id (same cache + graceful-degrade
  pattern as `EnrichVesselAsync`: on a failed vessel call, return jobs without vessel names, don't fail the page).
- Add `Title`, `RequestCode`, `VesselName` to the BFF job DTO (`Contracts/Jobs/GetProviderJobsResponse.cs`).

### Acceptance — JB-1
- `GET /provider/…/jobs` returns each job with `title`, `requestCode`, and `vesselName` (when the vessel resolves).
- One bulk vessel call per page (no N+1). A missing vessel → that job's `vesselName` null, others unaffected.
- No customer identity. Provider-scoped (unchanged).

---

## Phase JB-2 — Jobs summary counts (global)

Powers the KPI board and the "İş Durumu Dağılımı" donut. These are **global** counts across the provider's jobs —
the paginated list cannot produce them.

### 1. Module — grouped count query
- Add repo method `Task<IReadOnlyDictionary<ServiceRequestStatus,int>> GetStatusCountsByProviderAsync(long
  providerProfileId, CancellationToken ct)` → a single `GROUP BY status` over the provider's assignments (no full
  materialization, no N+1).
- New `GetProviderJobsSummaryQuery` + handler → response DTO:
  ```
  int Assigned, Scheduled, InProgress, WaitingForOwnerApproval, WaitingForMaterial, Paused,
      CompletionSubmitted, Completed;
  int Active;   // all non-Completed
  int Total;
  ```
  (Zero-fill missing statuses.) Provider id from the assertion, same as the list handler.

### 2. BFF — passthrough endpoint
- `GET /api/v1/provider/…/jobs/summary` → `GetProviderJobsSummaryBffQuery` → module. Provider-scoped
  (`ProviderActive`). Refit method on `IProviderServiceRequestRemoteCall`. No enrichment needed (counts only).

### Acceptance — JB-2
- `GET …/jobs/summary` returns the counts above for the calling provider (globally, not just the current page).
- The four KPI cards derive from it: Aktif = `Active`, Devam Eden = `InProgress`, Planlanan = `Scheduled`,
  Onay Bekleyen = `WaitingForOwnerApproval + WaitingForMaterial + CompletionSubmitted`.
- Donut = the per-status breakdown. One grouped query; provider-scoped; no customer data.

---

## Constraints (both phases)
- Provider-scoped; ownership enforced in the module (assertion → `ProviderProfileId`). No client-sent profile id.
- Modules never call each other — SR fields come from the in-module join; vessel name is the only BFF enrichment.
- One bulk vessel call per page (no N+1); summary is one grouped query. Dates stay UTC/Postgres-safe.
- Status values stay enum codes (the SPA localizes labels). No customer identity anywhere.

## Report
Append to `REPORT_BACKEND.md` ("JB-1", "JB-2"): a job row showing title/requestCode/vesselName (+ that one bulk
vessel call was made), and the summary counts for the seeded provider (with the KPI mapping). Unfinished is **not
done**.

## Frontend (I will build after these land)
FE-1 (KPI board from JB-2, skeleton until then) + FE-4 (enriched tabbed table from JB-1). Charts (FE-2, needs
recharts + JB-3 workload) and the action panel (FE-3, needs JB-4) follow.
