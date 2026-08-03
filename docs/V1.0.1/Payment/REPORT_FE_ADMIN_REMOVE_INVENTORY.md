# REPORT — FE_ADMIN remove the orphan `/app/inventory` (Envanter) page

**Repo:** `inktavia-marine-admin-web` (FE only)
**Spec:** `docs/V1.0.1/Payment/FE_ADMIN_REMOVE_INVENTORY_PAGE.md`
**Scope:** Pure deletion + reference cleanup of a foundation placeholder page that called
`GET /inventory/items` — an endpoint that does not exist in `addesso-project` (the only inventory-like backend
is CargoDry provider inventory, an unrelated concept). The page only ever rendered `ErrorState`.

## Files deleted
- `src/pages/app/InventoryStockPage.tsx`
- `src/features/inventory/` (whole dir — contained only `hooks/useInventoryListQuery.ts`)
- `src/shared/i18n/locales/en/inventory.json`
- `src/shared/i18n/locales/tr/inventory.json`

## References removed
- **`src/app/layouts/DashboardLayout.tsx`** — removed the Ticaret-group nav item
  `{ key: 'inventory', icon: 'warehouse', labelKey: 'inventory', path: ROUTES.INVENTORY }`.
  Left the unrelated `icon: 'inventory_2'` on the other item untouched.
- **`src/app/router/routeObjects.tsx`** — removed the `InventoryStockPage` import and the route
  `{ path: 'inventory', element: <InventoryStockPage /> }`.
- **`src/app/router/routes.tsx`** — removed `INVENTORY: '/app/inventory'`.
- **`src/shared/i18n/i18n.ts`** — removed both `import enInventory …` / `import trInventory …` and the two
  namespace registrations `inventory: enInventory` / `inventory: trInventory`.
- **`src/shared/i18n/namespaces.ts`** — removed `INVENTORY: 'inventory'`.
- **`src/shared/i18n/locales/tr/navigation.json`** — removed `"inventory": "Envanter"`.
- **`src/shared/i18n/locales/en/navigation.json`** — removed `"inventory": "Inventory"`.

## Left alone (per spec)
- `src/pages/app/DashboardPage.tsx:74` mock list entry `{ …, type: 'inventory' }` — demo dashboard data, a string
  label, not a route/page reference. The dashboard "Critical Inventory" KPI tile and "Stock Health" chart are the
  same kind of demo widgets and are also out of scope.

## Grep-clean confirmation
Repo-wide grep for `InventoryStockPage`, `useInventoryListQuery`, `ROUTES.INVENTORY`, `/app/inventory`,
`@features/inventory`, `features/inventory`, `'/inventory/items'`, and the `inventory` i18n namespace usages
(`useTranslation('inventory')`, `NAMESPACES.INVENTORY`, `enInventory`, `trInventory`) → **zero** remaining matches.

Remaining `inventory` substring hits are all unrelated and intentionally left:
- `src/shared/api/types/report.types.ts` / `src/pages/app/ReportsPage.tsx` — `'inventory-snapshot'` report type.
- `src/entities/provider/types/provider.types.ts` — `inventoryRows` (provider CargoDry).
- `src/pages/app/DashboardPage.tsx:74` — the demo mock string.
- Various CargoDry files (`cargodry` — separate provider-inventory concept).

## Build health
- `npm run typecheck` (`tsc --noEmit`) → **clean**, 0 errors.
- `npm run lint` → no new issues in any touched file. Pre-existing repo lint errors remain in unrelated files only
  (`src/pages/public/LoginPage.tsx`, `test/helpers/auth.helper.ts`, `test/scripts/generateHtmlReport.ts`); none are
  introduced or affected by this change.

## On-screen verification
Dev server (`vite`, port 3000), logged-in admin:
- **Ticaret** sidebar group shows **Paketler**, **Ödemeler**, **Finans & Raporlar** — **no Envanter**.
- Navigating to `/app/inventory` no longer resolves → redirects to the app **404 / not-found** page
  ("The page you are looking for does not exist").
- No other page regressed.
