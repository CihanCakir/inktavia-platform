# REPORT — FE_ADMIN sidebar nav restructure (collapsible "Ödemeler" + "Finans & Raporlar")

**Repo:** `inktavia-marine-admin-web` (FE only). **Scope honored:** navigation + a small Sidebar capability + i18n
only. No routes/pages/logic changed; backend/BFF, provider-web, CargoDry untouched. Stitch design preserved.

## What was the problem
The `/app/payments` dashboard had grown a wall of tiles because the payment sub-pages had **no sidebar access** — the
sidebar's `payments` entry was a single link to the dashboard. The finance/report pages (`/app/finance/*`, `/app/reports`)
had routes but **no menu entry at all**. Fixed by making the sidebar the primary nav.

## Change 1 — `src/shared/ui/sidebar/Sidebar.tsx`: collapsible sub-items
Extended `NavItem` with `children?: NavItem[]` and added collapsible rendering (leaf items render exactly as before):

- **Expanded rail (240px):** a parent renders as a button row (icon + label + `expand_more` chevron that rotates 180° on
  open) toggling an indented `<ul>` of child links with a left divider border. Rendering is **recursive** (`renderParent`
  calls itself for nested children), so a two-level submenu is supported if ever needed — we shipped one level.
- **Active state:** child links use a best-match helper — a child is active only if its path is the **longest** matching
  prefix of the current location (`matchesPath` = exact or descendant). This makes the "Genel Bakış" overview (`/app/payments`)
  highlight only on the exact route, not on every sub-page. The parent shows an accent (active) state when **any** child
  matches.
- **Auto-expand + persistence:** open state is `openMap[key] ?? hasActiveChild` — a parent **auto-opens** on load when a
  child route is active (deep links land with their menu open), and the user's explicit toggle wins thereafter. Because
  the desktop `<Sidebar>` instance persists across SPA route changes, navigating within a submenu **does not collapse it**.
- **Collapsed rail (80px):** parents render as icon-only buttons that open a **fixed-position flyout/popover** of children
  on hover/focus. Fixed positioning (coords from the button's `getBoundingClientRect()`) is used deliberately so the
  flyout **escapes the aside's `overflow-y-auto` horizontal clipping**. Accessible: `aria-haspopup`, `aria-expanded`,
  `role="menu"`/`menuitem`, focus/blur handling.
- Mobile drawer uses the same `<Sidebar collapsed={false}>`, so it gets the expanded collapsible behavior for free.

## Change 2 — `src/app/layouts/DashboardLayout.tsx`: NAV_GROUPS regroup
In the **commerce** group:
- Replaced the single `payments` link with a **collapsible `payments` parent** (icon `account_balance_wallet`) whose 19
  children are, in order: Genel Bakış · İşlemler · Gateway Logları · Komisyon Kuralları · Platform Ücreti · Kâr Koruma ·
  Müşteri İndirimleri · Fayda Bütçesi · Komisyon Avantajı Kuralları · Komisyon Avantajı Hakları · İade Dağıtım Politikaları ·
  İade Kuyruğu · Ters İbraz Kuyruğu · Sağlayıcı Bakiyeleri · Sub-Merchant KYC · Abonelik Planları · Kullanıcı Abonelikleri ·
  Hak Takibi · Sağlayıcı Ödemeleri. All paths use existing `ROUTES` constants.
- Added a **new collapsible `finance` parent** ("Finans & Raporlar", icon `monitoring`) with 7 children: Finansal Raporlama
  (`FINANCE_REPORTING_DASHBOARD`) · Mutabakat Genel Bakış (`FINANCE_OVERVIEW`) · Komisyon Kuralı Kullanımı
  (`FINANCE_COMMISSION_RULE_USAGE`) · Fatura Ekstresi (`FINANCE_INVOICE_STATEMENT`) · CargoDry Settlement
  (`FINANCE_CARGODRY_SETTLEMENT_RECONCILIATION`) · CargoDry Yenileme (`FINANCE_CARGODRY_RENEWAL_RECONCILIATION`) · Raporlar
  (`REPORTS`).
- **Folded the standalone `reports` item** (formerly in the `system` group) into the finance menu and removed the duplicate
  top-level entry, so finance/report pages are now menu-reachable without a redundant link.

**One-level vs two-level:** shipped the **ordered one-level** submenu. The Sidebar render is recursive so two-level nesting
is available, but one level reads cleanly at 19 items with the divider indent and keeps risk low, as the spec allowed.

## Change 3 — i18n + icons
`src/shared/i18n/locales/{tr,en}/navigation.json` — added `finance` label ("Finans & Raporlar" / "Finance & Reports"),
plus `paymentsChildren.*` (19 keys) and `financeChildren.*` (6 keys) with full tr/en parity. Reused the existing `reports`
key for the folded Raporlar item. Each item has a Material-symbols icon consistent with the existing set (e.g.
`receipt_long`, `rule`, `percent`, `shield`, `undo`, `gavel`, `verified_user`, `subscriptions`, `insights`, `balance`,
`autorenew`).

## Quality gates
- `npm run typecheck` (tsc --noEmit): **clean**.
- `eslint` on the two changed `.tsx` files: **clean** (no warnings/errors).
- No changes to routes, pages, backend/BFF, provider-web, or CargoDry.

## On-screen verification (fresh admin login — `admin.user@inktavia.com`, dev OTP)
> Auth note: the first login attempts 500'd at Keycloak with `different_user_authenticated` — a **stale Keycloak SSO
> session** for another user was present (infra, unrelated to this change). Cleared it via the Keycloak logout endpoint,
> then the OTP login completed. Recorded here for the next person; no code change made for it.

1. **Sidebar structure:** Ticaret group now shows **Ödemeler** and **Finans & Raporlar** as collapsible parents (chevrons);
   all pre-existing top-level items unchanged; the old standalone Raporlar is gone from System.
2. **Ödemeler expand:** clicking it rotates the chevron and reveals the indented list of all 19 payment sub-pages.
3. **Deep link auto-expand + active child:** navigating directly to `/app/payments/refund-queue` loaded the İade Kuyruğu
   page with **Ödemeler auto-expanded** and **İade Kuyruğu highlighted** (parent shown in accent).
4. **SPA persistence:** with Ödemeler open, clicking **İşlemler** navigated in-app to `/app/payments/transactions`; the
   submenu **stayed open** and İşlemler became the active child.
5. **Finans & Raporlar:** deep link to `/app/finance/financial-reporting` **auto-expanded the finance menu**, highlighted
   Finansal Raporlama, and rendered the Financial Reporting dashboard (previously unreachable from the menu). Other finance
   children (Mutabakat, Komisyon Kuralı Kullanımı, Fatura Ekstresi, CargoDry Settlement/Yenileme, Raporlar) present.
6. **Collapsed 80px rail flyout:** collapsing the sidebar and hovering the Ödemeler icon showed a fixed **flyout popover**
   ("ÖDEMELER" header + all children in full labels, İşlemler highlighted) that renders fully without being clipped by the
   rail's overflow.
7. **Mobile drawer:** uses the same expanded `<Sidebar>`, so it inherits the verified collapsible behavior.

## Optional (not done)
Left the `PaymentDashboardPage` tile wall as-is — slimming it was flagged nice-to-have; the nav is the deliverable and the
tiles no longer hurt now that every sub-page is one click away in the sidebar. Trivial follow-up if desired.
