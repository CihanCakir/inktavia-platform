# FE_ADMIN — remove the orphan `/app/inventory` (Envanter) page (no backend)

> **Repo:** `inktavia-marine-admin-web` (FE only). `/app/inventory` (`InventoryStockPage`) is a **foundation
> placeholder with no backend**: it calls `GET /inventory/items`, but there is **no Commerce/Inventory module and no
> `/inventory/items` endpoint** in `addesso-project` (the only inventory-like backend is CargoDry provider inventory,
> `/cargodry/...`, a different concept). The query has no `placeholderData`, so the page just renders `ErrorState`.
> **Decision (owner):** remove it now; it will be rebuilt cleanly when the future **Commerce** module lands.
>
> Pure deletion + reference cleanup. No backend, no other pages. `npm run typecheck` + lint clean.

## Delete
- `src/pages/app/InventoryStockPage.tsx`
- `src/features/inventory/` (the whole dir — `hooks/useInventoryListQuery.ts` and any siblings)
- `src/shared/i18n/locales/en/inventory.json` and `src/shared/i18n/locales/tr/inventory.json`

## Remove references
- **`src/app/layouts/DashboardLayout.tsx`** — remove the Ticaret-group nav item (line ~55):
  `{ key: 'inventory', icon: 'warehouse', labelKey: 'inventory', path: ROUTES.INVENTORY }`.
  (Leave the unrelated `icon: 'inventory_2'` on line ~28 — that's a different item's icon, not this route.)
- **`src/app/router/routeObjects.tsx`** — remove the `InventoryStockPage` import (line ~87) and the route
  `{ path: 'inventory', element: <InventoryStockPage /> }` (line ~202).
- **`src/app/router/routes.tsx`** — remove `INVENTORY: '/app/inventory'` (line ~86).
- **`src/shared/i18n/i18n.ts`** — remove `import enInventory …` (line ~18), `import trInventory …` (line ~41), and the two
  `inventory: enInventory` / `inventory: trInventory` namespace registrations (lines ~70, ~94).
- **`src/shared/i18n/namespaces.ts`** — remove `INVENTORY: 'inventory'` (line ~14).
- **`navigation.json`** (tr + en) — remove the `inventory` label ("Envanter" / "Inventory").

## Leave alone
- `src/pages/app/DashboardPage.tsx:74` has a mock list entry `{ …, type: 'inventory' }` — this is demo dashboard data,
  a string label, **not** a reference to the removed page/route. Leave it (removing it is out of scope; it doesn't break).

## QA
- Repo-wide grep for `InventoryStockPage`, `useInventoryListQuery`, `ROUTES.INVENTORY`, `/app/inventory`,
  `@features/inventory`, `'/inventory/items'`, and the `inventory` i18n namespace → **zero** remaining references (except
  the DashboardPage mock string above). No dead imports.
- Ticaret sidebar group no longer shows **Envanter**; visiting `/app/inventory` no longer resolves (router default /
  404). `npm run typecheck` + lint clean; no other page regressed.

## Verification (on-screen)
Fresh admin login: sidebar Ticaret shows **Paketler** + **Ödemeler** + **Finans & Raporlar** but **no Envanter**;
`/app/inventory` doesn't resolve. typecheck/lint clean.

## Report
`docs/V1.0.1/Payment/REPORT_FE_ADMIN_REMOVE_INVENTORY.md`: files deleted, references removed, grep-clean confirmation,
on-screen note.
