# Admin QA — Service Requests + Maintenance (A-QA4)

Routes: `/app/service-requests` (+ `/:id`, `/:id/offers`, `/:id/work-logs`, `/:id/dispute`), `/app/maintenance-schedules`.
Sidebar: Servis Talepleri ▾ (overview + disputes child). Not mock-intercepted — all hit the real BFF.

## Static findings
| Screen | State | Issues |
|---|---|---|
| `ServiceRequestsListPage` | REAL (`useServiceRequestsListQuery`/stats) | **dead buttons** `+ NEW REQUEST` (266), `EXPORT` (270), `FILTERS` (286), Category/Vessel chips (302/317), `VIEW ALL ALERTS` (535) — no onClick; **100% hardcoded EN** |
| `ServiceRequestDetailPage` | REAL (detail+timeline+status) | **Edit** (418)/**Suspend** (547)/**Internal Memo save** (851) are toast-only, not persisted; Contact/View-Profile link generically (no id); mixed i18n (FX/travel use `t`, rest EN) |
| `OffersMonitorPage` | **STUB/mock overlay** | 🔴 hardcoded `$4,275.00 Held` (USD); fake `SR-2024-0892`/`M/Y Stella Maris`/dates + `FOOTER_CARDS`; `useOffersQuery` hits bespoke `GET /service-requests/:id/offers` (**404/500 risk** — real offers ship in SR detail); redundant with SR-detail OffersModal |
| `WorkLogsPage` | REAL (logs+add-entry) | dead header/`INITIATE COMMS` buttons; hardcoded EN |
| `CompletionDisputeReviewPage` | PARTIAL | see Disputes/ (raw reason enum + dead resolution buttons) |
| `MaintenanceSchedulesPage` | **REAL, exemplary** (list/upsert/active + vessels + lookups, full i18n) | none — not in nav (child) |

- Numeric-enum: SR status/offer status correctly mapped in the api layer (`STATUS_INT_MAP`/`OFFER_STATUS_INT_MAP`,
  `resolveOfferStatus`). Money defaults TRY (except OffersMonitor USD).

## Live walkthrough checklist
- [ ] List filters/new/export actually work (or are removed); category/vessel filters populated.
- [ ] Detail Edit/Suspend/Internal-Memo **persist** to the BFF (not toast-only); Contact/View-Profile carry the right id.
- [ ] OffersMonitor: retire it or fix the endpoint + remove USD/fake data (confirm no 404 in console on `/:id/offers`).
- [ ] Work-logs add-entry works; dead header buttons removed/wired.
- [ ] SR list/detail fully localized tr/en.
- [ ] Maintenance schedules: create/edit/activate a schedule end-to-end.

## Fix candidates
`FIX_A_QA4_SR_DEAD_ACTIONS` (persist edit/suspend/memo; wire list actions), `FIX_A_QA4_OFFERS_MONITOR` (retire or fix +
de-USD), `FIX_A_QA4_SR_I18N`.
