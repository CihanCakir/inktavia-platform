# BE_QA4 — SR detail state-vs-text gaps (timeline empty, scheduled-window, stats)

> **Repos:** `addesso-project` (ServiceRequest module + AdminPanel BFF) + a small `admin-web` labeling touch. The SR
> detail shows **"Henüz zaman çizelgesi olayı yok."** even though the SR is **Scheduled + assigned + 2 offers**, and
> the list KPIs / velocity / alerts look inert. Close the state-vs-text gaps. **Do not commit.**

## A — Timeline is empty (primary)
**Root cause (from code):** `GetServiceRequestTimelineQueryHandler` maps `sr.StatusHistory` → timeline events (and the
detail page's fallback also reads `data.statusHistory`). For SR 9011 **`StatusHistory` is empty**, so both the
`/timeline` endpoint and the fallback render nothing — even though the SR clearly went Open → OfferReceived → Assigned →
Scheduled.

Two things to confirm/fix in the **ServiceRequest module**:
1. **Transitions must append `StatusHistory`.** Verify the domain `ChangeStatus` (and each command that moves the SR —
   `AssignProvider`, `AcceptServiceRequestOffer`, `StartServiceRequestAssignment`, `UpdateServiceRequestStatus`,
   completion/dispute handlers) records a `StatusHistory` entry (fromStatus, toStatus, actorUserId, reason,
   occurredAt UTC). If any transition path skips it, add it in the domain method so **every** status change is recorded
   once (idempotent, no duplicates).
2. **Backfill seed SRs.** Seed SRs (like 9011) were inserted directly at their terminal status with **no** history →
   nothing to show. Add a **history backfill** for seeded SRs (a migration/seed step that writes the plausible
   transition chain up to the current status, UTC timestamps), so the admin timeline is populated for demo data.
   (Duplicate-safe: only when `StatusHistory` is empty for that SR.)

After this, the detail timeline shows the real lifecycle; the FE already renders `timeline.events` (and the
`statusHistory` fallback) correctly and is now localized.

## B — Scheduled-window shows three different date ranges (labeling, low severity)
The detail surfaces **three legitimately different** dates that currently read as inconsistent:
- Top strip **Start/End** = `requestedStartDate/EndDate` (owner-**requested**, Jul 15).
- Assigned-provider card **"Planlandı"** = `assignment.scheduledStartDate/EndDate` (**scheduled**, Jul 18→20).
- Offers drawer **schedule** = offer `estimatedStartDate/EndDate` (provider-**estimated**, Jul 20→22).

These are different fields, not a data bug — but they look contradictory unlabeled. **Fix (FE labeling only):** label each
clearly via i18n — "Talep edilen" (requested) / "Planlanan" (scheduled) / "Tahmini" (estimated) — so the admin reads them
as distinct. No data change.

## C — List KPIs / velocity / alerts (BFF stats)
- **"AKTIF ONARIM 0"**: the FE computes `stats?.activeRepairs ?? items.filter(i => i.status === 'inprogress').length`.
  Confirm the AdminPanel BFF **stats endpoint** actually computes `activeRepairs` (SRs in `inprogress`) — if it returns
  null the FE falls back to the current page only. Ensure the stats endpoint returns real `totalRequests`,
  `activeRepairs`, `criticalAlerts` from the module (not page-local).
- **"Onarım Hızı — Hız verisi yok." / "Son Uyarılar — Son uyarı yok."**: the stats endpoint returns empty
  `repairVelocity`/`recentAlerts`. Either wire these (a simple per-day completed-count series + recent
  critical/warning events) or, if out of scope, keep the empty-state (now localized) and note it as deferred. Decide
  and document.

## Don't-break / QA
- History recording is additive in the domain (record once per transition; UTC); backfill is idempotent seed/migration
  data, no financial/logic change. Stats wiring is read-only. FE labeling is i18n-only.
- **Tests:** (1) a status transition writes exactly one `StatusHistory` row; (2) the backfill populates seed SRs and is
  re-run-safe; (3) `/timeline` for 9011 returns the ordered events; (4) stats endpoint returns non-null
  activeRepairs/totals.
- **Live (admin :3001):** `/app/service-requests/9011` → the timeline lists the real transitions (not the empty state);
  the KPI strip reflects real counts; the three date ranges are clearly labeled.

## Report
`docs/V1.0.1/Admin/REPORT_BE_QA4_SR_STATE_GAPS.md`: the status-history recording/backfill, the stats wiring (or the
documented deferral), the date-labeling, and the live proof the timeline is populated. Cross-link
[[admin_sr_pages_i18n_qa]]. QA-6 (SignalR) is separate.
