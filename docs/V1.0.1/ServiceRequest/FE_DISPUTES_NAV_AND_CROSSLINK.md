# FE — disputes under the Service Requests menu + SR-detail ↔ dispute cross-linking

> **Repo:** `inktavia-marine-admin-web` only. Two navigation improvements: (1) move `/app/disputes` into the **Service
> Requests** menu as a **collapsible submenu** child; (2) **cross-link** the service-request detail and the dispute case so
> an admin hops between them easily. Pure FE — no backend, no route-path changes (reuse the existing routes). Additive;
> tr + en. **Do not commit** until the user says.

## Current state (investigated)
- Routes already exist: `disputes` (list `DisputesPage`), `disputes/:serviceRequestId/:disputeId/case`
  (`DisputeCasePage`), `service-requests` (list), `service-requests/:serviceRequestId` (`ServiceRequestDetailPage`),
  `service-requests/:serviceRequestId/dispute` (legacy `CompletionDisputeReviewPage`).
- Nav is defined in **`src/app/layouts/DashboardLayout.tsx`** (the rendered sidebar; supports `children` for collapsible
  submenus — see `cargodry` and `packages`) and mirrored in **`src/app/router/navigation.ts`** (`NavItem` with
  `children?`). The `service-requests` item is currently **flat** (no children). `ROUTES.DISPUTES = '/app/disputes'`.
- `DisputeCasePage` already has a breadcrumb back to `ROUTES.DISPUTES`.

## Part 1 — `/app/disputes` as a submenu under Service Requests
- In **`DashboardLayout.tsx`**, convert the `service-requests` item to a parent **with `children`** (mirror the
  `cargodry`/`packages` collapsible pattern): the parent keeps `path: ROUTES.SERVICE_REQUESTS`, and its children are at
  least:
  - **Service Requests** (the list) → `ROUTES.SERVICE_REQUESTS` (the overview child, like `cargodry-overview`),
  - **Disputes** → `ROUTES.DISPUTES` (icon e.g. `gavel` / `balance`).
  (If other SR sub-pages deserve promoting later — offers/work-logs — leave them; scope is adding **Disputes** cleanly.)
- Mirror the change in **`navigation.ts`** if that structure is also consumed (keep the two in sync; add the `children`
  there too). Add the required-permission on the Disputes child matching the SR read permission.
- **i18n:** add submenu labels under the navigation namespace (e.g. `serviceRequestsChildren.overview`,
  `serviceRequestsChildren.disputes`) in **tr + en**, following the `cargodryChildren.*` convention.
- Active-state: the Service Requests group should show **expanded/active** when on `/app/disputes` or a
  `/app/disputes/**/case` route (so the submenu highlights correctly). Verify the existing active-match logic covers the
  disputes children (extend if it only matches the parent path prefix).

## Part 2 — cross-link SR detail ↔ dispute case
- **From `ServiceRequestDetailPage`:** when the service request **has a dispute**, show a clear entry point (a card/button
  in the detail header or a status banner) — "İtiraz görüntüle / View dispute" — navigating to the **dispute case**
  `ROUTES.DISPUTE_CASE(serviceRequestId, disputeId)`. Get the `disputeId` from whatever the detail already exposes about
  the SR's dispute; if the detail payload doesn't carry it, use the existing **dispute-by-service-request** lookup (the
  module has `GetByServiceRequestIdAsync`; the disputes list is also filterable) — a light query, no new backend. Only show
  the link when a dispute exists; if the SR is disputed but the case route needs a disputeId that isn't available, fall
  back to the disputes list filtered to that SR.
- **From `DisputeCasePage`:** in addition to the existing "back to Disputes" breadcrumb, add a clear link to the **service
  request detail** (`ROUTES.SERVICE_REQUEST_DETAIL(serviceRequestId)`) — e.g. on the SR summary section ("Servis talebine
  git / Go to service request"). The `serviceRequestId` is already in the route params.
- Keep the existing dispute list → case navigation working.

## Don't-break / QA
- Pure FE, additive: nav restructure + two cross-links + i18n. No route paths changed, no backend, no data change. Existing
  Service Requests list, dispute list, and case page keep working; the legacy `service-requests/:id/dispute` page is
  untouched. admin-web tsc + eslint clean; tr + en parity for new keys.

## Verify (on screen)
1. The sidebar shows **Service Requests** as a collapsible group with **Disputes** as a child; clicking Disputes opens
   `/app/disputes`; the group highlights/expands when on the disputes list or a case page.
2. Opening a **disputed** service request's detail shows a visible link into its dispute case; clicking it lands on the
   correct `DisputeCasePage`.
3. On the dispute case page, a link navigates to the matching service-request detail.
4. Non-disputed SR detail shows no dispute link; existing navigation unaffected.

## Report
`docs/V1.0.1/ServiceRequest/REPORT_FE_DISPUTES_NAV.md`: the submenu restructure (DashboardLayout + navigation.ts + active
state), the SR-detail → case and case → SR-detail links (+ how the disputeId is resolved), the i18n keys, and the on-screen
check. **Do NOT commit.**
