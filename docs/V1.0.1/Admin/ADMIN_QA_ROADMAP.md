# Admin Panel — Go-Live QA Roadmap (V1.0.1)

> **Repo:** `inktavia-marine-admin-web` (runs at `http://localhost:3000`) + its BFF `addesso-project/Bff/src/AdminPanel`
> (`bff-adminpanel`, `localhost:17001`). The **last comprehensive QA pass before go-live**: audit every admin screen for
> broken/non-working areas, mock/placeholder data, currency (₺/USD), numeric-enum leaks, stale nav, and i18n, then fix them
> module by module. This master holds the findings + the phased plan; each module has a matching `docs/V1.0.1/Admin/<Module>/`
> subdirectory with a `QA_FINDINGS.md` (static findings + live-walkthrough checklist + planned fix). **Audit only — no fixes
> in this pass; no commit.** Mirrors the completed Provider QA (`docs/V1.0.1/Provider/`).

## Method + scope note
Seeded by a **static/code audit** of the whole app (routes `routeObjects.tsx`, the real sidebar in
`DashboardLayout.tsx`, per-feature api/hooks/pages, mock constants, `USD`/hardcoded/TODO greps, enum mappers, i18n locales)
**plus a live logged-in pass** at `localhost:3000` (admin `admin.user@inktavia.com`, OTP login, console attached). Static
reliably surfaces structure/wiring/mock/stale-status; the live pass confirmed the headline issues (dashboard USD, mock
payment screens, CargoDry English, provider raw-enum badges, finance = gold standard) and caught a realtime error. Each
module phase still ends with a **screen-by-screen live walkthrough** (checklist in each module dir).

## App map
**Real sidebar** = `src/app/layouts/DashboardLayout.tsx` `NAV_GROUPS` (Turkish labels), groups:
- **ANA** — Gösterge Paneli (dashboard)
- **OPERASYONLAR** — Gemiler (vessels), Kullanıcılar (users), Sağlayıcılar (providers), Servis Talepleri ▾ (service-requests + disputes + maintenance-schedules), CargoDry ▾ (16 children)
- **TİCARET** — Paketler ▾ (packages + user-subscriptions), Ödemeler ▾ (19 payment screens), Finans & Raporlar ▾ (finance 7)
- **İLETİŞİM** — Mesajlar (messages)
- **SİSTEM** — Performans (performance), Bildirimler (notifications→templates), Referans Veri (→lookup-groups), Pricing Attributes, Ayarlar (settings)

~90 routes total in `routes.tsx`. Detail/`:id` routes reached from lists. **Not in any sidebar (URL-only):** file-storage, identity/organizers, identity/venues, identity/participants, commerce/orders, commissions, reference-data/currencies, notifications/inbox (bell only).

## Cross-cutting findings (X-series — fix first; they distort the whole app)

### X0 — Admin OTP login on-screen dead-end / handoff (🔴 GO-LIVE BLOCKER — verify)
The BFF `POST /auth/otp-login/verify` returns `nextAction: "redirect_to_keycloak_handoff"` **with a valid `loginTicket`**
(confirmed via curl → the backend handoff is live). But a live UI attempt showed the blocked banner *"Kod doğrulandı ancak
tek kullanımlık kod ile giriş henüz yapılandırılmadı"* (the FE `keycloak_handoff_required` copy) and did **not** redirect —
i.e. the admin-web handoff execution (Phase 2b) is not reliably completing the Keycloak redirect. (Login DID eventually
succeed for this audit, so it's intermittent/partial, not total.) **Confirm the FE consumes `redirect_to_keycloak_handoff`
→ Keycloak → `/auth/callback` cleanly for a fresh admin; this gates every authenticated screen.** Root cause candidate:
FE nextAction handling in the OTP verify flow. See `Navigation/QA_FINDINGS.md`.

### X1 — Two divergent nav sources; documented one is DEAD; no permission gating (HIGH)
`src/app/router/navigation.ts` (`navigationItems`, with `requiredPermission` per item) is **imported nowhere → dead code**.
The real sidebar is `DashboardLayout.tsx` `NAV_GROUPS`, which has **no `requiredPermission` and no permission filtering** in
`Sidebar.tsx` → **every nav item renders for every authenticated user** regardless of role (`PermissionRoute.tsx` exists but
routes are wrapped only in `ProtectedRoute`). Reconcile to one source and apply role gating. No item is badged "coming
soon", so there are no false "soon" badges — the mismatch is structural.

### X2 — Dashboard is not a real cockpit (HIGH) 🔴
`DashboardPage.tsx` is ~90% hardcoded mock. Only 2 of 4 metric cards are wired (`totalVessels`=14, `activeServiceRequests`=0
— confirmed live). **"Pending Payouts $124,500" and "Critical Inventory 7" are hardcoded**, and all four charts + the
Recent-SR / Pending-Approvals ($4,200/$12,000/$8,500) / Recent-Messages lists are hardcoded consts. The BFF
`DashboardOverview` already exposes `pendingReviews`, `pendingServiceRequests`, `recentActivity` — unused. Rebuild as a real
cockpit (₺). See `Dashboard/`.

### X3 — Currency ₺ (TRY) vs USD (HIGH — the provider "X4" bug) 🔴
Marketplace settles TRY. Live/static USD leaks:
- **Dashboard** hardcoded `$124,500` + `$4,200/$12,000/$8,500` (confirmed live). 🔴
- **CargoDry create-form default is USD** (`CargoDryProductsPage.tsx:40` `currencyCode:'USD'`; picker lists USD first,
  `:119`). Seeded products currently display **TRY** live (TRY 250/300/…), so this is a **latent** bug — new products/renewals
  would bill USD. Renewal pages render raw `currencyCode` inherited from product (`:154/164/169`).
- **OffersMonitorPage** hardcoded `$4,275.00 Held`.
- **Currency data model is USD-pegged**: `currency.types.ts` `exchangeRateToUsd`; currencies screen column "Rate (USD)".
- **PaymentTransactionDetail mock** rows `currencyCode:'USD'`; **payments i18n** has `amountUsd` key.
- Latent trap: `shared/lib/formatters/currencyFormatter.ts` defaults `'USD'`/`en-US` (currently no callers).
- ✅ Correct TRY: all finance pages (`tr-TR`), all CargoDry **commercial**/settlement pages (`tr-TR`), rule-CRUD payment
  pages, provider finance panel, dispute case.

### X4 — Numeric-enum serialization → raw-number/raw-Pascal renders (MEDIUM–HIGH)
AdminPanel BFF serializes enums as **numeric codes** (and for many modules also ships `*Name` strings). Mappers exist:
`disputeEnums.ts`, `messagingEnumMaps.ts`, `ledgerEnumMaps.ts`, `payments/queues/enumMaps.ts`. **Correctly mapped:** disputes,
messaging, finance ledger, payment queues (refund/chargeback/KYC/balances/allocation), CargoDry commercial (uses `*Name`),
pricing-attributes (`dataTypeName`). **Leaks / risks:**
- **Providers table badges** render raw Pascal `Approved`/`Active` untranslated (confirmed live — Pascal-vs-lowercase variant
  miss); `ProviderDetailPage` same. `ORGANIZASYON="SelfEmployed"` raw.
- **CompletionDisputeReviewPage:282** renders `Reason Code: {n}` raw (the one true numeric leak) + ad-hoc `String(status)==='2'`.
- **Identity** organizers/venues/participants render `row.status` raw (local `STATUS_VARIANT`, untranslated).
- **Rule-CRUD payment lists** + transactions/payouts/user-subscriptions render `{row.status}` via string-keyed maps — will
  blank/leak if the DTO emits ints (several `payment.types.ts` fields are typed `number`). Verify against live payloads.

### X5 — Mock / stub screens despite a live backend (HIGH) 🔴
Pages shipping fabricated data while their backend exists:
- **Payments → Gateway Logs** — 100% mock (confirmed live: 14,282/142/28, fake "Stripe Marine / Marine Pay / Global Swift"
  gateways, EVT_99xx). Marketplace uses iyzico — fake Stripe branding is a go-live risk.
- **Payments → Entitlements Monitoring** — 100% mock (`MOCK_ENTITLEMENTS/MATRIX`).
- **Payments → Commission Calculation detail** — 100% mock; ignores `:id`.
- **Commerce → Orders** — empty stub behind a feature flag (English hardcoded).
- **Users → transactions tab** always empty (`.catch(()=>[])`); **Approvals** fabricates risk (`id%10`) + dates.
- **Vessel documents** Approve/Replace are `console.log('[TODO]')`; upload buttons no handler.
- **Dashboard** (X2). **OffersMonitorPage** (fake SR code/vessel/dates + `$` + likely-404 endpoint).

### X6 — i18n: hardcoded-English pages + missing keys (MEDIUM)
tr/en **namespace parity is clean** (22 namespaces both sides, matching key counts). The debt is **pages that don't call
`t()`** and **keys used-but-absent (silent English fallback)**:
- **CargoDry: 19 of 23 pages 100% hardcoded English** (entire commercial/settlement cluster, kit detail/list, batch,
  products, qr, lifecycle, alerts, renewal prep/detail) — largest surface; no `tr` strings exist for them.
- Hardcoded-English pages: SR list/offers-monitor/work-logs/completion-dispute-review, notifications inbox (no `notifications`
  namespace at all), identity organizers/venues/participants, file-storage, performance risk-watchlist + participant-detail,
  settings profile/account/security sub-pages, commissions, commerce/orders.
- Missing keys (English fallback): dashboard `metrics.*` mismatch, reports (2-key namespace), reference-data currencies.

### X7 — Realtime (SignalR) negotiation fails on load (MEDIUM)
Live console on dashboard: `Failed to start the connection: The connection was stopped during negotiation.` The messaging
realtime hub fails to negotiate — verify messages/moderation realtime actually connects (may fall back to poll or go stale).

## Per-module audit table (static + live)
| Module | Route(s) | Real state | Nav | Action |
|---|---|---|---|---|
| **Dashboard** | `/app/dashboard` | 2/4 cards real; rest hardcoded mock + **USD** | ANA | **Cockpit rebuild + ₺** (X2/X3) |
| **Navigation** | (nav infra) | dead `navigation.ts`; no perm gating; orphaned routes; **login handoff X0** | — | Reconcile nav + gate + fix login (X0/X1) |
| **Payments** | `/app/payments/*` (~20) | mostly REAL + enum-mapped; **3 mock screens** (gateway-logs, entitlements, commission-calc); raw-enum risk on rule lists | Ödemeler ▾ | Wire 3 mock screens; verify enum payloads; fake-Stripe branding |
| **Commissions (legacy)** | `/app/commissions` | orphaned, likely-404 `/commissions/summary`, dead payout button, hardcoded EN | — | Delete or rewire to `/payment/*` |
| **Finance** | `/app/finance/*` (7) | **REAL, TRY, i18n — gold standard** (live-confirmed); minor: USD/EUR filter option, page-only KPI sums | Finans ▾ | Light QA; drop USD/EUR filter; full-set totals |
| **Packages** | `/app/packages` | REAL (₺); activate/deactivate **no `onError`** → silent 400 on seed plans; cargodry/commerce tabs = ComingSoon | Paketler ▾ | Add error handling; hide stub tabs |
| **Commerce/Orders** | `/app/commerce/orders` | **STUB** empty div behind flag; hardcoded EN | orphaned | Build or hide |
| **Reports** | `/app/reports` | REAL KPIs; labels hardcoded; 2-key i18n ns | SİSTEM(naveq) | i18n keys |
| **CargoDry** | `/app/cargodry/*` (~23) | all REAL-wired; **19 pages no-i18n**; **USD create-default** (seeded=TRY live); UI-only status strips | CargoDry ▾ | i18n cluster; product default →TRY; null-guard renewalPrice |
| **ServiceRequests** | `/app/service-requests/*` | list/detail/work-logs REAL; many **dead buttons**; **OffersMonitor mock+USD**; hardcoded EN | Servis Talepleri ▾ | Wire dead actions; retire/fix OffersMonitor; i18n |
| **Maintenance Schedules** | `/app/maintenance-schedules` | REAL, exemplary, i18n | child | Live QA only |
| **Disputes** | `/app/disputes` (+case) | **REAL, exemplary, enum-mapped, TRY, i18n** | child | Live QA; but **CompletionDisputeReview** raw enum + 3 dead resolution buttons |
| **Messages** | `/app/messages/*` (4) | **REAL, exemplary, realtime, enum-mapped, i18n** | Mesajlar | Fix **SignalR negotiation X7**; live QA |
| **Notifications** | `/app/notifications/{inbox,templates}` | inbox REAL but **no i18n ns**; templates **404-risk** (unconfirmed endpoint, flag) + route mounted unconditionally | Bildirimler | Add `notifications` ns; gate/verify templates endpoint |
| **Identity** | `/app/identity/{organizers,venues,participants}` | REAL but **no i18n**, **orphaned nav**, raw status enums | orphaned | Nav + i18n + enum map |
| **Users** | `/app/users/*` | REAL list/detail; **BFF TODOs** (email/vesselCount blank/0); **perf tab dead** (`!=='provider'`); transactions tab empty; approvals **fabricated risk/dates** | Kullanıcılar | Backend TODO fields; fix perf-tab guard; stop fabricating data |
| **Providers** | `/app/providers` (+`:id`) | REAL; **raw Pascal status badges** untranslated (live) | Sağlayıcılar | Map/normalize approval+status badges |
| **Vessels** | `/app/vessels/*` | REAL; **"Export Registry" dead**; **document Approve/Replace = TODO stubs**, upload no handler | Gemiler | Wire export + document actions |
| **Files** | `/app/file-storage` | REAL but minimal (no search/filter/paging), **no i18n**, **orphaned nav** | orphaned | Nav + i18n + controls |
| **Performance** | `/app/performance/*` (6) | REAL; **broken Lucide icons** on risk-watchlist + participant-detail; 2 pages no-i18n | Performans | Fix icon names; i18n |
| **Reference Data** | `/app/reference-data/*` | pricing-attributes REAL; lookup-items REAL (broken breadcrumb link); **lookup-groups dead Add buttons**; **currencies orphaned + USD-model + dead Edit/fake Sync** | Referans Veri | Wire lookup CRUD; currency ₺ model; nav |
| **Settings** | `/app/settings/*` | `/settings` = language card only; profile/account/security = **"coming soon" stubs** (no i18n) | Ayarlar | Build or hide sub-pages |

## Phases (module by module — each = a `docs/V1.0.1/Admin/<Module>/` dir; highest-impact first)
> Each phase: static findings already captured in the module dir → build fix kickoff(s) → implement → **live walkthrough** →
> `REPORT_*`. Sequenced by go-live impact.

- **A-QA0 — Navigation & Login** (`Navigation/`): confirm/fix the OTP→Keycloak handoff (X0, blocker); reconcile the two nav
  sources to one + apply permission gating (X1); decide the orphaned routes (add to nav vs hide). Unblocks everything.
- **A-QA1 — Dashboard** (`Dashboard/`): rebuild as a real ₺ cockpit — wire pending payouts/approvals/disputes/refund-queue/
  KYC/SR KPIs, remove hardcoded USD + mock charts (X2/X3).
- **A-QA2 — Payments mock screens** (`Payments/`): wire Gateway Logs, Entitlements, Commission-Calculation-detail to their
  live BFF (or hide if endpoints truly absent — note iyzico gate); remove fake "Stripe" branding; verify rule-list enum
  payloads (X4/X5). Delete/rewire legacy `/app/commissions`.
- **A-QA3 — CargoDry currency + i18n** (`CargoDry/`): product create default USD→TRY + align renewal formatters to ₺/tr-TR
  (X3); i18n the 19 hardcoded pages (largest surface); null-guard renewalPrice.
- **A-QA4 — Service Requests** (`ServiceRequests/`): wire the dead buttons (new/export/filters, edit/suspend/memo persist,
  release-payment/mediate/rectify → P10/dispute); retire or fix OffersMonitor (mock+USD+404 risk); i18n.
- **A-QA5 — Identity / Users / Providers** (`Identity/`): nav + i18n for organizers/venues/participants; provider raw-enum
  badges; user BFF TODO fields + perf-tab guard + stop fabricated approvals data.
- **A-QA6 — Vessels & Files** (`Vessels/`): wire vessel-document Approve/Replace/upload + export; file-storage nav + i18n +
  list controls.
- **A-QA7 — Reference Data & Settings** (`ReferenceData/`): lookup-groups Add/edit handlers + breadcrumb link; currency ₺
  model + nav; build/hide settings sub-pages.
- **A-QA8 — Performance** (`Performance/`): fix broken Lucide icon names (Material Symbols ligatures); i18n 2 pages.
- **A-QA9 — Messages & Notifications** (`Messages/`): fix SignalR negotiation (X7); add `notifications` i18n namespace;
  gate/verify notification-templates endpoint.
- **A-QA10 — Disputes** (`Disputes/`): the list/case are exemplary — just fix `CompletionDisputeReviewPage` (raw reason enum
  + 3 dead resolution buttons + counter-evidence upload).
- **A-QA11 — Finance polish** (`Finance/`): drop USD/EUR filter options (or scope to TRY); full-dataset KPI totals; Packages
  activate/deactivate `onError`; build/hide Commerce/Orders + Reports i18n keys.

## Go-live gate
Definition of done: (1) a fresh admin can log in through the UI (X0 confirmed); (2) the sidebar is one truthful,
permission-gated source with no orphaned/unreachable screens (X1); (3) the dashboard is a real ₺ cockpit (X2); (4) no USD on
any settling surface (X3); (5) no raw numeric/Pascal enum leaks (X4); (6) no mock/stub screen shipped as if real (X5).
**Externally gated (out of this pass):** live iyzico payment surfaces (P9 keys) — gateway-logs/transactions realism;
VAT/KDV reporting numbers (YMM sign-off) on the finance dashboard.
