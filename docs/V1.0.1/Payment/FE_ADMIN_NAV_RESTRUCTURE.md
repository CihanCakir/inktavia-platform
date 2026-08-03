# FE_ADMIN — sidebar nav restructure: collapsible "Ödemeler" submenu + "Finans & Raporlar" main menu

> **Repo:** `inktavia-marine-admin-web` (FE only). The `/app/payments` dashboard has grown a long wall of tiles because
> the payment sub-pages have **no sidebar access** — the sidebar's `payments` entry is a single link to the dashboard.
> Restructure the sidebar so **Ödemeler** is a **collapsible parent** exposing all payment sub-pages, and add a
> **Finans & Raporlar** collapsible main menu (the finance/report pages currently have routes but **no menu entry at
> all**). **No routes/pages change — this is navigation + a small Sidebar capability + i18n.**
>
> **Do NOT touch** backend/BFF, provider-web, CargoDry, or any page component's logic. Keep the Stitch design.

## Ground truth (confirmed)
- **The live sidebar menu is `NAV_GROUPS` in `src/app/layouts/DashboardLayout.tsx`** (passed to `<Sidebar groups=…>`).
  Groups: core / operations / commerce (has `payments` — a single link) / communications / system (has `reports`).
  There is **no finance entry**. (`src/app/router/navigation.ts` is a separate/legacy list — the Sidebar uses
  `NAV_GROUPS`; align/ignore navigation.ts but don't rely on it.)
- **`src/shared/ui/sidebar/Sidebar.tsx` renders `group.items` as flat links and does NOT render `children`** → add
  collapsible sub-item rendering.
- All routes already exist in `src/app/router/routeObjects.tsx` (paths below). Use the existing `ROUTES` constants where
  present; add new constants (or literal `/app/...` paths) for the sub-pages that lack them.

**Payment sub-pages (under `/app/payments/...`):** `` (dashboard) · `transactions` · `gateway-logs` ·
`commission-rules` · `platform-fee-rules` · `profit-protection-policies` · `customer-discount-rules` ·
`customer-benefit-budget-policies` · `commission-benefit-rules` · `commission-benefit-entitlements` ·
`refund-allocation-policies` · `refund-queue` · `chargeback-queue` · `provider-balances` · `sub-merchant-kyc` ·
`subscription-plans` · `user-subscriptions` · `entitlements` · `provider-payouts`.
**Finance/report pages (under `/app/finance/...` + reports):** `financial-reporting` (dashboard) · `` (reconciliation
overview) · `commission-rule-usage` · `invoice-statement` · `cargodry/settlements` · `cargodry/renewals` ·
`/app/reports`.

## Change 1 — Sidebar.tsx: collapsible sub-items
Extend the Sidebar's `NavItem` to support `children?: NavItem[]`. When an item has children, render it as a
**collapsible parent**: a button row (icon + label + a chevron that rotates on expand) that toggles a nested,
indented `<ul>` of child links. Requirements:
- **Active state:** a child link highlights when its route is active; the parent shows an active/accent state when any
  child route is active, and **auto-expands** on load if a child route is active (so a deep link opens with its menu
  open). Persist the open/closed state across route changes (local component state or a small store) so navigating
  within a submenu doesn't collapse it.
- **Collapsed sidebar (80px) mode:** parents with children show a **flyout/popover** of the children on hover/focus
  (don't try to expand inline in the 80px rail). Keep it accessible (aria-expanded, keyboard).
- Leaf items (no children) render exactly as today. Don't regress the existing groups/labels.

## Change 2 — DashboardLayout NAV_GROUPS: regroup
Restructure `NAV_GROUPS` so the payment + finance surfaces are menu-accessible:
- In the `commerce` group, replace the single `payments` link with a **collapsible `payments` parent** whose children
  are the payment sub-pages, in a sensible order — e.g.:
  Genel Bakış (`/app/payments`) · İşlemler (`…/transactions`) · Gateway Logları (`…/gateway-logs`) ·
  Komisyon Kuralları (`…/commission-rules`) · Platform Ücreti (`…/platform-fee-rules`) ·
  Kâr Koruma (`…/profit-protection-policies`) · Müşteri İndirimleri (`…/customer-discount-rules`) ·
  Fayda Bütçesi (`…/customer-benefit-budget-policies`) · Komisyon Avantajı Kuralları (`…/commission-benefit-rules`) ·
  Komisyon Avantajı Hakları (`…/commission-benefit-entitlements`) · İade Dağıtım Politikaları
  (`…/refund-allocation-policies`) · İade Kuyruğu (`…/refund-queue`) · Ters İbraz Kuyruğu (`…/chargeback-queue`) ·
  Sağlayıcı Bakiyeleri (`…/provider-balances`) · Sub-Merchant KYC (`…/sub-merchant-kyc`) ·
  Abonelik Planları (`…/subscription-plans`) · Kullanıcı Abonelikleri (`…/user-subscriptions`) ·
  Hak Takibi (`…/entitlements`) · Sağlayıcı Ödemeleri (`…/provider-payouts`).
- Add a **collapsible `finance` parent** (a NEW main menu — in `commerce` or `system`, your call for balance) whose
  children are: Finansal Raporlama (`/app/finance/financial-reporting`) · Mutabakat Genel Bakış (`/app/finance`) ·
  Komisyon Kuralı Kullanımı (`/app/finance/commission-rule-usage`) · Fatura Ekstresi (`/app/finance/invoice-statement`) ·
  CargoDry Settlement (`/app/finance/cargodry/settlements`) · CargoDry Yenileme (`/app/finance/cargodry/renewals`) ·
  Raporlar (`/app/reports`). (Fold the existing standalone `reports` item into this finance menu, or leave it — but
  ensure finance/report pages are reachable from the menu.)
- **Optional (recommended if the 19-item Payments submenu feels long):** use **two-level nesting** under Ödemeler —
  sub-groups "Kurallar & Politikalar", "İşlemler", "Operasyon Kuyrukları", "Abonelik & Ödemeler" — since `children`
  is recursive. If you do this, make the Sidebar render nested children too. If it adds too much risk, ship the
  ordered one-level submenu.

## Change 3 — i18n + icons
- `navigation.json` (tr+en): add labels for the `payments` parent, the `finance` parent, and every sub-item key — full
  parity. Reuse existing keys where a label already exists (e.g. `reports`).
- Give each item a Material-symbol icon consistent with the current set (the sidebar uses string icon names like
  `account_balance_wallet`, `insights`).

## Optional (nice-to-have, mention don't force)
- Slim the `PaymentDashboardPage` tiles now that the sidebar is the primary nav — the dashboard can become a compact
  landing (KPIs + a few shortcuts) instead of the full tile wall. Only if quick; the nav is the deliverable.

## Don't-break / QA
- Existing top-level items (dashboard, vessels, users, providers, service-requests, cargodry, inventory, packages,
  messages, performance, analytics, notifications, reference-data, settings) unchanged and still work.
- Route access unaffected (paths already exist); this only adds menu entries. Deep links still land correctly and now
  auto-open their submenu. Collapsed sidebar + mobile drawer both handle the new collapsible items.
- `npm run typecheck` + lint clean; provider-web/CargoDry untouched; no backend changes.

## Verification (on-screen)
Fresh admin login. The sidebar shows **Ödemeler** as an expandable parent → clicking it reveals the payment sub-pages;
navigating to any of them keeps the menu open and highlights the active child; deep-linking to e.g.
`/app/payments/refund-queue` opens with Ödemeler expanded + that item active. **Finans & Raporlar** appears as a new
expandable menu exposing the financial-reporting dashboard + the other finance/report pages (previously unreachable from
the menu). Collapsed sidebar shows flyouts; mobile drawer works. typecheck/lint clean.

## Report
`docs/V1.0.1/Payment/REPORT_FE_ADMIN_NAV_RESTRUCTURE.md`: the Sidebar collapsible capability, the new NAV_GROUPS
(Ödemeler children + Finans & Raporlar menu), one-level vs two-level choice, i18n keys added, and the on-screen
transcript.
