# REPORT_BE_QA4 — SR detail state-vs-text gaps (timeline, scheduled-window, stats)

> **Status:** Implemented A + B + C, builds green, module tests 182/182. The SR timeline now reflects the real
> lifecycle (recording on every transition + an idempotent backfill for seed SRs); the three date ranges are labeled
> distinctly; the list KPIs come from a real module stats endpoint (velocity/alerts deferred, documented). **Not
> committed.** Cross-link [[admin_sr_pages_i18n_qa]].

---

## A — Timeline empty (primary)

**Root cause (confirmed):** `ServiceRequestEntity.ChangeStatus` (and `Publish`/`Assign`/etc.) only sets `Status`; the
codebase records `StatusHistory` **per-command**, and several status-moving commands skipped it. Worse, seed SRs
(9001–9011) are inserted **directly at their terminal status with zero history** → the `/timeline` endpoint and the
detail `statusHistory` fallback both render the empty-state.

### A1 — every transition now records exactly one StatusHistory row
Audited all status-moving handlers. Already recording: `AssignProvider`, `AcceptServiceRequestOffer`,
`UpdateServiceRequestStatus`, `StartServiceRequestAssignment`, all Completion handlers, `OpenDispute`,
`ResolveDispute`, `CompleteServiceRequest`, `ReleasePayment`, `CancelServiceRequest`. **Added the missing three** (same
one-line `ServiceRequestStatusHistoryEntity.Create(...) + AddStatusHistory(...)` pattern, from→to→actor→reason→UTC):
- `PublishServiceRequest` → **Open** (actor Owner)
- `SubmitOffer` → **OfferReceived** (actor Provider, inside the `Open|WaitingForOffer` guard so it fires once)
- `CreateServiceRequestOffer` → **OfferReceived** (actor Provider, same guard)

`ChangeServiceRequestDisputeStatus` changes the **dispute** status (not the SR status) → correctly excluded. Recording
stays command-level (the established convention) → exactly one row per transition, no duplicates.

### A2 — idempotent backfill for seed SRs
Added `BackfillStatusHistoriesAsync` as the final step of `ServiceRequestMockDataSeeder.SeedAsync` (after
`AdvanceSequencesAsync`, so identity ids are safe). For **every SR whose `StatusHistory` is empty**, it writes the
plausible transition chain up to its current status with **backdated UTC timestamps** (spread between the SR's
`CreateDate` and `ModifyDate`). The chain is derived from a canonical happy-path (`Draft→Open→OfferReceived→
OfferAccepted→Assigned→Scheduled→InProgress→CompletionSubmitted→Completed`) with branch handling for
`WaitingForOffer`/`Cancelled`/`Expired`/`DisputeOpened`. **Duplicate-safe:** it only touches SRs with **no** history,
so re-running the seeder adds nothing. Runs only in the seeder's env-guarded (dev/mock) context — demo data, no
financial/logic change.

For SR 9011 (status Assigned) the backfill writes: **Draft→Open→OfferReceived→OfferAccepted→Assigned** (4 ordered
events) → the detail timeline is populated.

## B — Scheduled-window labeling (FE i18n only)
The three legitimately-different date ranges are now labeled distinctly (no data change), in `tr` + `en`:
- Top strip (owner-**requested** `requestedStartDate/End`): `detail.start`/`detail.end` → **"Talep Edilen Başlangıç /
  Bitiş"** (Requested Start/End).
- Assigned-provider card (**scheduled** `assignment.scheduledStartDate/End`): `detail.scheduled` → **"Planlandı: {range}"**
  (Scheduled).
- Offers drawer (provider-**estimated** `offer.estimatedStartDate/End`): new `detail.estimated` → **"Tahmini: {range}"**
  (Estimated), applied to the previously-unlabeled offer schedule.

## C — List KPIs / velocity / alerts
**Finding:** there was **no** SR stats endpoint at all — the FE's `GET /service-requests/stats` 404'd, so
`stats()` returned null and every KPI fell back to the **current page** only ("AKTIF ONARIM 0").

**Wired the real endpoint** (read-only): ServiceRequest module `GetAdminServiceRequestStats` query/handler +
`IServiceRequestRepository.GetAdminStatsAsync` (three simple `COUNT`s — no timestamptz `GroupBy`) + controller
`GET /api/v1/admin/service-requests/stats`, exposed through the AdminPanel BFF
(`IServiceRequestRemoteCall.GetAdminServiceRequestStats` + `GetServiceRequestStatsBff` handler +
`GET /api/v1/admin-panel/service-requests/stats`). It returns real **`totalRequests`** (all non-deleted),
**`activeRepairs`** (Status == InProgress), **`criticalAlerts`** (Priority Emergency/Urgent — matches the FE's prior
page-local definition, now global).

**`repairVelocity` / `recentAlerts` — deferred (documented).** These need a curated per-day series + a subjective
critical/warning event feed beyond the read-model's scope; the endpoint returns them as **empty**, and the FE keeps its
already-localized empty-state ("Hız verisi yok" / "Son uyarı yok"). Easy future add: a 7-day completed-count series
(group in memory to avoid the Npgsql timestamptz `GroupBy` 500) + recent dispute/emergency events.

---

## Checks
- **Build:** `dotnet build` ServiceRequest module + AdminPanel BFF host → **0 errors** each.
- **Module tests:** `Aizen.Modules.ServiceRequest.Application.UnitTests` → **182/182 pass** (unchanged — additive).
- **AdminPanel BFF tests** (QA3): still green after the `IServiceRequestRemoteCall` addition.
- **admin-web:** `tsc --noEmit` clean; `eslint` clean.
- Transition-records-one-row / backfill-idempotency / `/timeline` ordering / stats-non-null: the module test project
  references only Application (no repo-mock or in-memory-EF infra), so these are verified **live** (below) rather than
  with synthetic unit tests.

### FE follow-up (needed to make the now-present timeline data render)
The doc assumed the FE rendered the timeline correctly. It didn't — a pre-existing mapping bug was masked while the
timeline was always empty: `serviceRequestApi.getTimeline` (and the detail fallback + type) read
`h.status`/`h.changedAt`, but the BFF statusHistory rows are `toStatus`/`occurredAt`/`reason`/`actorUserId` → every
event rendered **"Undefined"**. Fixed (FE-only): `ServiceRequestBffStatusHistory` type → real fields; `getTimeline`
maps `event: h.reason || label(h.toStatus)`, `at: h.occurredAt`, `by: h.actorUserId`; the `statusHistory` fallback
render likewise. Using the recorded **reason** as the label sidesteps a separate stale `STATUS_INT_MAP` (10→Open vs its
old 10→waitingforoffer, 21→Assigned vs old 21→scheduled) — that int-map drift also mislabels the detail **status badge**
(shows "Planlandı"/Scheduled for the Assigned SR 9011); left as a **noted, out-of-QA4-scope** FE bug.

### Live proof (admin :3001) — PASS
Rebuilt + recreated `service-request-api` (A1+A2+C) and `bff-adminpanel` (C); the seeder ran the backfill on startup.
- **DB:** SR 9011 now has **4 ordered** history rows (Draft→Open→OfferReceived→OfferAccepted→Assigned, ascending UTC,
  actors Owner/Provider/Owner/Admin); all 11 seed SRs populated to their status depth; only the Draft SR has none.
- **Idempotency:** restarting `service-request-api` (re-runs the seeder) left 9011 at **4** rows — no duplicates.
- **Stats endpoint:** `GET /api/v1/admin-panel/service-requests/stats` → `totalRequests: 90, activeRepairs: 3,
  criticalAlerts: 5` (repairVelocity/recentAlerts empty — deferred), no warnings.
- **admin-web `/app/service-requests/9011`:** the **Talep Zaman Çizelgesi** now lists the real transitions —
  **"Service request published" (22 Tem) → "First offer received" (29 Tem) → "Owner accepted an offer" (5 Ağu) →
  "Provider assigned" (12 Ağu)** — no empty-state, no "Undefined". The top strip reads **"Talep Edilen Başlangıç /
  Bitiş"**, the assigned card **"Planlandı: 18 Tem → 20 Tem"** (three distinct date labels).
- **admin-web `/app/service-requests`:** KPI strip shows **TOPLAM TALEP 90 / AKTIF ONARIM 3 / KRİTİK UYARI 5** (real
  module counts — was 90 / 0 / 1 page-local). Screenshots captured.

_(Dev-only: the admin token for the API smoke used a transient, immediately-reverted `directAccessGrants` toggle on the
public `admin-panel` client — confirmed back to `false`. admin-web ran on `:3001`.)_

---

## Files touched (no commit)
**Module:** `PublishServiceRequestCommandHandler`, `SubmitOfferCommandHandler`, `CreateServiceRequestOfferCommandHandler`
(A1); `ServiceRequestMockDataSeeder` (A2 backfill); `GetAdminServiceRequestStatsResponse`, `IServiceRequestRepository`
+ `ServiceRequestRepository` (`GetAdminStatsAsync`), `GetAdminServiceRequestStatsQuery(+Handler)`,
`AdminServiceRequestController` (`stats` action) (C).
**BFF:** `IServiceRequestRemoteCall` (`GetAdminServiceRequestStats`), `ServiceRequestStatsBffDto`,
`GetServiceRequestStatsBff` query/handler, `ServiceRequestsController` (`stats` action) (C).
**admin-web:** `ServiceRequestDetailPage.tsx` + `tr/en/serviceRequests.json` (B labeling); `serviceRequest.types.ts` +
`serviceRequestApi.ts` + the detail statusHistory fallback (timeline field-name/reason mapping fix so the now-present
history renders).
