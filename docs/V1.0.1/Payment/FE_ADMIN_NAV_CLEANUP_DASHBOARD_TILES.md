# FE_ADMIN — nav cleanup: slim the Payment dashboard + move CargoDry sub-links into a collapsible menu

> **Repo:** `inktavia-marine-admin-web` (FE only). Follow-up to the sidebar restructure. Now that the sidebar exposes
> the payment sub-pages, the `/app/payments` dashboard's in-page **sub-link tile wall is redundant** → remove it. Do the
> **same treatment for CargoDry**: its landing/dashboard pages carry an in-page sub-link section — move those into a new
> **collapsible CargoDry menu** (reusing the Sidebar's collapsible capability from the last change) and remove the
> in-page tiles.
>
> **Do NOT touch** backend/BFF, provider-web, or **CargoDry business logic** — this is navigation + removing redundant
> in-page nav sections only. Keep the Stitch design. `npm run typecheck`+lint clean.

## Part A — slim `PaymentDashboardPage`
`src/pages/app/payments/PaymentDashboardPage.tsx` currently has a **sub-link tile section** (the block of tiles that
`navigate(...)` to PlatformFeeRules / ProfitProtection / CustomerDiscount / BenefitBudget / CommissionBenefit /
Entitlements / ProviderPayouts / RefundQueue / ChargebackQueue / ProviderBalances / KYC / RefundAllocationPolicies —
around the tiles that duplicate the sidebar). **Remove that redundant navigation-tile section** — the sidebar's Ödemeler
menu is now the way in.
- **Keep** the real content: the **KPI cards** (they show live figures; keeping their click-through is fine as a
  shortcut) and the **data DashboardCards** (status distribution, recent transactions, etc.). The page becomes a
  compact landing (KPIs + data), not a nav wall.
- If a KPI card only exists to navigate (no figure), drop it too; if it shows a metric, keep it. Judgment call — the
  goal is "no redundant sub-link grid", not "strip the dashboard bare".

## Part B — CargoDry: collapsible menu + remove in-page sub-links
1. **Sidebar menu (`DashboardLayout.tsx` NAV_GROUPS):** replace the single `cargodry` link with a **collapsible
   `cargodry` parent** (reuse the `children` capability already in the Sidebar). Children = the CargoDry sub-pages
   (paths already in `routeObjects.tsx`), in a sensible order — e.g.:
   Kitler (`/app/cargodry`) · Kit Listesi (`/app/cargodry/kits`) · Ürünler (`/app/cargodry/products`) · Partiler
   (`/app/cargodry/batches`) · Yaşam Döngüsü Olayları (`/app/cargodry/kits/lifecycle-events`) · Operasyonel Uyarılar
   (`/app/cargodry/kits/alerts`) · QR Sorgu (`/app/cargodry/qr-lookup`) · Yenileme Adayları
   (`/app/cargodry/renewals/candidates`) · Yenileme Hazırlıkları (`/app/cargodry/renewals`) · Fırsat Yönlendirme
   (`/app/cargodry/opportunity-routing-preview`) · Ticari Panel (`/app/cargodry/commercial`) · Satış Atıfları
   (`/app/cargodry/commercial/sales-attributions`) · Settlement'lar (`/app/cargodry/commercial/settlements`) · Settlement
   Otomasyonu (`/app/cargodry/commercial/settlement-automation`) · Kural Çözüm Önizleme
   (`/app/cargodry/commercial/rules/resolve-preview`). Use the existing `ROUTES.CARGODRY*` constants. (Consolidate any
   stray `cargodry-commercial` top-level item into this parent.)
   - **Optional (recommended if long):** two-level nesting — sub-groups **Envanter** (kits/products/batches/
     lifecycle/alerts/qr), **Yenileme** (candidates/preparations), **Ticari** (commercial dashboard/sales-attributions/
     settlements/automation/rules-preview) — the Sidebar already renders nested children.
2. **Remove the in-page sub-link sections** from the CargoDry landing + commercial dashboard pages where they duplicate
   the menu — notably `CargoDryCommercialDashboardPage.tsx` (the tile block that `navigate(...)` to sales-attributions /
   settlements / settlement-automation / rules-resolve-preview) and any quick-link/tile grid on the `/app/cargodry`
   landing. **Keep** each page's actual data/KPIs/tables; only the redundant navigation grids go. Do not change any
   CargoDry data logic, queries, or commercial calculations.

## Part C — i18n + icons
`navigation.json` (tr+en): add a `cargodryChildren` block with labels for every CargoDry sub-item (mirror the existing
`paymentsChildren`/`financeChildren` structure), full parity. Give each a consistent Material-symbol icon.

## Don't-break / QA
- Every removed in-page link still reachable via the sidebar menu (verify each destination has a menu entry before
  removing its tile). Deep links unaffected; the CargoDry menu auto-expands on a CargoDry route (the Sidebar already
  does this for children).
- Existing top-level items + the Ödemeler/Finans menus from the last change unchanged. Collapsed rail flyouts + mobile
  drawer work for the new CargoDry parent. typecheck+lint clean; backend/BFF/provider-web untouched; CargoDry logic
  untouched.

## Verification (on-screen)
Fresh admin login. `/app/payments` shows a compact landing (KPIs + data), **no sub-link tile wall**; every former tile
target is reachable from the Ödemeler menu. **CargoDry** is now a collapsible sidebar menu exposing its sub-pages
(active child highlighted, auto-expand on a CargoDry route); the CargoDry landing/commercial pages no longer show the
redundant in-page nav grid but keep their data. typecheck/lint clean.

## Report
`docs/V1.0.1/Payment/REPORT_FE_ADMIN_NAV_CLEANUP.md`: what was removed from the Payment dashboard, the CargoDry menu
(children + one-level vs two-level), the in-page sections removed from CargoDry pages, i18n keys added, on-screen
transcript.
