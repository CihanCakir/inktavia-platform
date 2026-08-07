# Admin Panel — Go-Live QA Kickoff (audit + roadmap generator)

> **Repo:** `inktavia-marine-admin-web` (runs at `http://localhost:3000`) + its BFF `addesso-project/Bff/src/AdminPanel`.
> This kickoff tells you to **produce the admin-panel go-live QA**: a comprehensive audit of every admin screen + a phased
> roadmap with per-module subdirectories — **mirroring the completed Provider QA** (`docs/V1.0.1/Provider/`). This pass is
> **audit + roadmap only — do NOT fix code here**; fixes come later as separate `FIX_*` phase kickoffs (like the provider
> P-QA* series). **Do not commit.**

## Deliverables (create these)
1. `docs/V1.0.1/Admin/ADMIN_QA_ROADMAP.md` — the master: method note, app map (routes/nav groups), **cross-cutting findings
   (X-series)**, a **per-module audit table** (module · route · real state · nav flag · action), and **phases** (A-QA0…n)
   sequenced highest-impact first.
2. `docs/V1.0.1/Admin/<Module>/QA_FINDINGS.md` — one subdirectory **per module needing work**, named after the feature,
   each with: static findings + a **live-walkthrough checklist** (screens to click + what to verify) + planned fix. Mirror
   the provider module-dir format exactly.

## Scope — audit every admin surface
Feature folders: `dashboard, payments (~15 sub-routes: commission-rules, platform-fee-rules, customer-discount-rules,
commission-benefit-rules/entitlements, customer-benefit-budget-policies, profit-protection-policies, refund-allocation-
policies, refund-queue, chargeback-queue, provider-balances, provider-payouts, part-commercial-terms, gateway-logs,
entitlements), finance (financial-reporting, commission-rule-usage, invoice-statement, cargodry/settlements, cargodry/
renewals), commissions, cargodry (~20 sub-routes: kits, products, batches, renewals, commercial/settlements+automation+
attributions, analytics, qr-lookup, opportunity-routing, alerts, lifecycle-events), service-requests, disputes,
maintenance-schedules, messages (+moderation, reports, support), notifications (inbox, templates), reference-data, lookup,
currencies, pricing-attributes, providers, identity (organizers, participants, venues), users, vessels, files, packages,
commerce/orders, settings, profile-performance`. Cover them all.

## What to check (per screen) — same lens as the provider QA
- **Broken / non-working:** runtime errors (attach the console/network tool **before** loading, then reload to catch
  load-time errors), 400/500s, empty views that should have data, dead buttons/links.
- **Stale nav flags / coming-soon:** any nav item marked planned/badged that's actually implemented (or vice-versa); any
  page that dead-ends on a "coming soon" stub while its backend exists.
- **Mock / placeholder / hardcoded:** leftover demo constants, placeholder metrics, mock data not wired to the real BFF.
- **Dashboard:** is it a real cockpit (real metrics, attention items) or placeholders? Propose improvements.
- **UI/UX + menu:** nav grouping/active-state, labels localized (tr+en parity — flag raw keys), consistent layout,
  loading/empty/error states, responsive issues.
- **Cross-cutting (learned from provider QA — check explicitly):**
  - **Currency (₺ TRY vs USD):** the marketplace settles in TRY; flag any admin screen showing USD (the provider X4 bug —
    CargoDry/finance had stale USD). Confirm one consistent TRY.
  - **Numeric-enum serialization:** the AdminPanel BFF serializes enums as **numeric codes**, not PascalCase names — flag any
    screen rendering a raw enum number instead of a label (needs an enum→name mapper, cf. `disputeEnums.ts`).
  - **i18n tr/en parity** on every screen.
- **Cross-reference the built backend:** the whole V1.0.1 economics (Payment P1–P12, SR S1–S13, dispute case, N1–N4, etc.)
  is done — verify each admin screen that should consume it actually does (correct figures, states), and flag anything
  still stubbed/mock despite a live backend.

## Method
- **Static pass:** routes, `navigation.ts`/sidebar, per-feature api/hooks/pages coverage, `PlannedPage`/coming-soon stubs,
  `USD`/hardcoded/mock/TODO greps, i18n gaps.
- **Live pass:** walk every screen at `localhost:3000` (logged-in admin), console attached before navigation; capture the
  real state, runtime errors, and visual/UX issues into the module `QA_FINDINGS.md`.
- **Do not fix** — record findings + a planned fix per module; the master roadmap sequences the fix phases (A-QA0…n).

## Report
The two deliverables above (master roadmap + per-module `QA_FINDINGS.md`). End with a prioritized phase list (highest-impact
/ go-live-critical first — e.g. currency, dashboard, any broken finance/payments screen, stale nav) and note items gated on
external inputs (iyzico keys for live payment screens, YMM for VAT). **Do not commit.** This is the pre-go-live admin
sign-off audit; fix phases follow as separate kickoffs.
