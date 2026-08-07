# Provider Web — Go-Live QA Roadmap (V1.0.1)

> **Repo:** `inktavia-marine-provider-web` (runs at `http://localhost:3002`). The **last comprehensive QA pass before
> go-live**: audit every provider screen for broken/non-working areas, stale "planned" state, unwired data, and UI/UX +
> menu issues, then fix them module by module. This master roadmap holds the findings + the phased plan; each module has a
> **matching subdirectory** under `docs/V1.0.1/Provider/<Module>/` for its detailed QA + fix kickoffs (same cadence as the
> other V1.0.1 tracks: one `FIX_*`/`FE_*` doc per slice + a `REPORT_*`).

## Method + scope note
This roadmap is seeded by a **static/code audit** of the whole app (routes, nav, per-feature api/hooks/pages, `planned`
flags, stubs, i18n). Static analysis reliably surfaces **structure, wiring, stale-status, and coming-soon stubs**; it does
**not** catch runtime errors or visual UI/UX regressions. So each module phase ends with a **live screen-by-screen
walkthrough** at `localhost:3002` (logged-in provider) to confirm the fix and catch runtime/visual issues. (Offer: the
walkthrough can be driven via the Chrome MCP if you want it automated.)

## App map (authenticated `/app/*`)
Routes: `dashboard`, `service-requests` (+ `/:id`, discovery), `offers`, `offer-library`, `jobs` (+ `/:id`), `messages`
(+ `/:threadId`), `finance` (+ `/payouts`), `performance`, `cargodry`, `renewals`, `products`, `inventory`, `documents`,
`notifications`, `profile`, `settings`, `support`. Onboarding + auth + status flows are separate.

Nav groups (sidebar): **work** (dashboard, service-requests, offers, offer-library, jobs, messages), **commerce**
(cargodry), **finance** (finance, performance), **account** (profile, documents, notifications, settings, support).

## Cross-cutting findings (fix first — they distort the whole app)

### X1 — Stale nav `status` flags → misleading "soon" badges (HIGH)
`navigation.ts` tags each item `implemented | planned`; `DesktopShell` renders a **"soon" badge** on every `planned` item.
But several `planned` items are **actually implemented**: **offers** (`OffersPage`, 4 query hooks — built this session with
S2–S5 economics), **notifications** (full `useNotifications`/preferences/push hooks + api). So working features wear a
"coming soon" badge → users think they're unavailable. **Reconcile every nav item's `status` with its real state** (audit
table below) and fix the badge.

### X2 — Dashboard is mostly placeholder metrics (HIGH — you flagged this)
`DashboardPage` renders **5 of 6 metric cards as `planned` placeholders** — `openOffers`, `serviceRequests`,
`unreadMessages`, `financeSnapshot`, `pendingRenewals` — with **no data**, even though all their backends now exist. Only
**jobs** is real. The dashboard should become a real cockpit (see the Dashboard module dir).

### X4 — Currency inconsistency ₺ (TRY) vs USD (HIGH — from live walkthrough) 🔴
Service Requests + Offers display **TRY (₺)**; **Finance + CargoDry display USD** (Hakediş "0 USD"; CargoDry "5.000 USD /
150 USD / 60 USD"). The marketplace settles in TRY → a provider seeing earnings/targets in USD is inconsistent and likely
wrong. Decide the canonical currency (TRY) and fix Finance + CargoDry (or make CargoDry-USD explicit + reconciled). Details:
`REPORT_LIVE_WALKTHROUGH.md`. **Go-live critical.**

### X3 — Genuine coming-soon stubs (`PlannedPage`) (MEDIUM)
**performance** and **documents** render the `PlannedPage` "Coming Soon" stub — genuinely unbuilt. Decide build-now vs
hide-from-nav-until-built (don't ship a nav item that dead-ends on "coming soon").

## Per-module audit table (static)
| Module | Route | Real state (static) | Nav flag | Action |
|---|---|---|---|---|
| **Dashboard** | `/app/dashboard` | jobs metric + jobs list real; 5 metric cards placeholder | implemented | **Wire real metrics + cockpit UX** |
| **Service Requests** | `/app/service-requests` | `DiscoveryPage`, 0 query hooks (discovery map) | planned | Verify/wire discovery + list; reconcile flag |
| **Offers** | `/app/offers` | `OffersPage`, 4 query hooks — **implemented** (S2–S5) | planned ❌ | Fix stale flag; live QA the economics/attributes/FX/travel/part surfaces |
| **Offer Library** | `/app/offer-library` | page, no api/hooks dir | implemented | Verify it's real (may use offers api) or reclassify |
| **Jobs** | `/app/jobs` (+`/:id`) | api+hooks+2 pages — implemented | implemented | Live QA (job detail, work-logs, evidence, complete) |
| **Messages** | `/app/messages` | api+hooks — implemented (realtime) | implemented | Live QA (realtime, unread, attachments, location) |
| **Finance** | `/app/finance` (+`/payouts`) | api+hooks+2 pages — implemented (PAY-0..5) | implemented | Live QA the 6 finance tabs; correctness vs snapshots |
| **Performance** | `/app/performance` | **`PlannedPage` stub** | planned | Build (profile-performance backend exists) or hide |
| **CargoDry** | `/app/cargodry` (+products/renewals/inventory) | 3 pages, api+hooks; products page 0 query hooks | planned | Verify wiring; provider CargoDry is read-oriented (per decision) |
| **Documents** | `/app/documents` | **`PlannedPage` stub** | planned | Build (FileStorage exists) or hide |
| **Notifications** | `/app/notifications` | full hooks+api — **implemented** | planned ❌ | Fix stale flag; live QA (list/read/preferences/push) |
| **Profile** | `/app/profile` | api+hooks — implemented | implemented | Live QA |
| **Settings** | `/app/settings` | page, **no api/ dir** | implemented | Verify it does something real or reclassify |
| **Support** | `/app/support` | api, no hooks | implemented | Live QA (live-support channel N-D) |

## Phases (module by module — each = a `docs/V1.0.1/Provider/<Module>/` dir)
> Sequenced highest-impact / most-visible first. Each phase: static findings already captured in the module dir → build the
> fix kickoff(s) → implement → **live walkthrough** → `REPORT_*`.

- **P-QA0 — Cross-cutting** (`Navigation/`): reconcile all nav `status` flags with reality (X1); decide stub items
  (hide-until-built vs build) (X3). Small, unblocks a truthful sidebar.
- **P-QA1 — Dashboard** (`Dashboard/`): wire the 5 placeholder metrics to their live backends (open offers, active service
  requests, unread messages, finance snapshot = pending payout/balance, pending renewals) + cockpit UX (quick actions,
  attention items, recent activity). (X2 — your explicit ask.)
- **P-QA2 — Offers** (`Offers/`): fix the stale flag; **live-QA the economics surfaces we built** (line breakdown, S2
  attributes picker, S3 FX display, S4 travel, S5 part allowance) — correctness + UX.
- **P-QA3 — Finance** (`Finance/`): live-QA the 6 finance tabs (settlements/payouts/transactions/invoices/subscription/
  profile) against the real snapshots; confirm figures + states.
- **P-QA4 — Jobs + Messages** (`Jobs/`, `Messages/`): live-QA the job workspace (detail, work-logs, evidence, complete) and
  messaging (realtime, unread, attachments, location).
- **P-QA5 — Service Requests / Discovery** (`ServiceRequests/`): verify/wire the discovery + list surface.
- **P-QA6 — Notifications** (`Notifications/`): fix the stale flag; live-QA list/read/preferences/push.
- **P-QA7 — Performance + Documents** (`Performance/`, `Documents/`): build the stubs (backends exist) or hide from nav.
- **P-QA8 — CargoDry** (`CargoDry/`): verify the read-oriented provider surface wiring.
- **P-QA9 — Account (Profile/Settings/Support)** (`Account/`): verify Settings does real work; live-QA profile + support.

## Module subdirectories (created)
`Dashboard/`, `Navigation/`, `Offers/`, `Finance/`, `Jobs/`, `Messages/`, `ServiceRequests/`, `Notifications/`,
`Performance/`, `Documents/`, `CargoDry/`, `Account/` — each seeded with a `QA_FINDINGS.md` (this module's static findings +
the live-walkthrough checklist to fill). Fix kickoffs land in the matching dir as `FIX_*` / `FE_*` docs.

## Go-live gate
This QA pass is the pre-go-live provider sign-off. It does **not** cover the iyzico live payment gate (P9 keys) or the
owner/customer app — those remain separately gated. Definition of done: every nav item is truthful (no false "soon"), the
dashboard is a real cockpit, and every implemented screen passes a live walkthrough with no runtime/visual blocker.
