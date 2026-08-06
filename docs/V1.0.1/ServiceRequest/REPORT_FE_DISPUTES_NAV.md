# REPORT — Disputes under Service Requests menu + SR-detail ↔ dispute-case cross-links

> `inktavia-marine-admin-web` only. Pure FE, additive; **no backend, no route-path changes** (reused existing routes).
> tr + en; tsc + eslint clean. **Not committed.**

## Part 1 — `/app/disputes` as a submenu under Service Requests

- **`src/app/layouts/DashboardLayout.tsx`** — the flat `service-requests` item is now a collapsible parent (mirroring the
  `cargodry`/`packages` pattern). Parent keeps `path: ROUTES.SERVICE_REQUESTS`; children:
  - `service-requests-overview` → `ROUTES.SERVICE_REQUESTS` (icon `list_alt`, label `serviceRequestsChildren.overview`)
  - `service-requests-disputes` → `ROUTES.DISPUTES` (icon `gavel`, label `serviceRequestsChildren.disputes`)
- **`src/app/router/navigation.ts`** — mirrored (kept in sync even though this list has no current consumer): added the
  same two `children`, each with `PERMISSIONS.SERVICE_REQUESTS_READ`.
- **i18n** (`navigation.json`, tr + en) — added `serviceRequestsChildren.overview` / `.disputes`
  (EN "Service Requests" / "Disputes"; TR "Servis Talepleri" / "Anlaşmazlıklar").
- **Active state** — no change needed: the Sidebar's existing `matchesPath` does prefix matching and `collectLeafPaths` /
  `bestMatch` already select the most-specific child, so the group auto-expands and highlights the **Disputes** child on
  both `/app/disputes` (list) and `/app/disputes/:sr/:dispute/case`, and the **overview** child on
  `/app/service-requests` and `/app/service-requests/:id`.

## Part 2 — cross-link SR detail ↔ dispute case

- **`src/pages/app/ServiceRequestDetailPage.tsx`** — when the detail payload carries `data.dispute` (the
  `ServiceRequestBffDispute`, which already includes `id`), the header actions now show a gold **"İtiraz görüntüle / View
  dispute"** link → `ROUTES.DISPUTE_CASE(id, String(data.dispute.id))`. The `disputeId` comes straight from the existing
  detail payload — **no new backend / no extra lookup**. Fallback: if `data.dispute` exists but `id` is somehow absent, the
  link degrades to `ROUTES.DISPUTES`. The link renders only when a dispute exists; the legacy
  `service-requests/:id/dispute` "Review/Completion & Dispute" links are untouched.
  - Fix applied while wiring: the main component had no `t` in scope (the file's other `useTranslation` lives in the
    `OffersModal` sub-component), so added `const { t } = useTranslation('serviceRequests')` to `ServiceRequestDetailPage`.
- **`src/pages/app/DisputeCasePage.tsx`** — the Service-request section header gains a **"Servis talebine git / Go to
  service request"** link → `ROUTES.SERVICE_REQUEST_DETAIL(srId)` (`srId` from route params), alongside the existing
  back-to-Disputes breadcrumb. Existing dispute-list → case navigation is unchanged.
- **i18n** (`serviceRequests.json` → `disputeCase`, tr + en) — added `viewDispute` (İtiraz görüntüle / View dispute) and
  `goToServiceRequest` (Servis talebine git / Go to service request).

## Files touched
- `src/app/layouts/DashboardLayout.tsx`
- `src/app/router/navigation.ts`
- `src/shared/i18n/locales/{en,tr}/navigation.json`
- `src/shared/i18n/locales/{en,tr}/serviceRequests.json`
- `src/pages/app/ServiceRequestDetailPage.tsx`
- `src/pages/app/DisputeCasePage.tsx`

## Verify (on screen — done)
1. **Sidebar** shows *Servis Talepleri* as a collapsible group with children *Servis Talepleri* (overview) + *Anlaşmazlıklar*
   (Disputes); clicking Disputes opens `/app/disputes`; the group is expanded and the Disputes child highlighted on both the
   disputes list and a `…/case` page (overview child highlighted on the SR list/detail).
2. **Disputed SR** (`/app/service-requests/9006`, "Dispute Opened") shows the **İtiraz görüntüle** button → lands on the
   correct `DisputeCasePage` `/app/disputes/9006/9901/case`.
3. **Case page** — **Servis talebine git** navigates to `/app/service-requests/9006`.
4. **Non-disputed SR** (`/app/service-requests/23`, "Waiting for Offer") shows **no** dispute link; the disputes list, case
   page, and legacy completion/dispute review links behave as before.

## QA
- Pure FE, additive: nav restructure + two cross-links + i18n. No route paths changed, no backend, no data change.
- admin-web **tsc + eslint clean**; navigation.json + serviceRequests.json **tr/en parity** confirmed.
