# CI-2b — provider CargoDry inventory table + movement ledger (frontend)

Replace the "Konsinye Stok Yönetimi" placeholder card on `CargoDryInventoryPage.tsx` with a real inventory table
and a movement ledger. Repo: `inktavia-marine-provider-web`. Keep the existing header, KPI board (CI-1), donut,
and alert banner untouched — only the placeholder region changes. Nautical Heritage tokens; no external chart/table
libs; `tsc -b` is the gate.

## 1. endpoints — `src/shared/api/endpoints.ts`
The `cargodry` block already has `inventory: '/cargodry/inventory'`. Add:
```ts
inventoryMovements: '/cargodry/inventory/movements',
```

## 2. API client + types — `src/features/cargodry/api/cargodryApi.ts`
Add types mirroring the BFF DTOs and two fetchers (envelope-unwrapping like the existing `getOverview`/`getAlerts`):

```ts
export interface CargoDryInventoryRow {
  id: number; providerProfileId: number; productCode: string; batchCode: string | null;
  commercialModel: number; commercialModelName: string;
  salesChannel: number; salesChannelName: string;
  totalAllocated: number; totalActivated: number; availableStock: number;
  lastMovementAtUtc: string | null; createdAtUtc: string;
}
export interface CargoDryInventoryPaged { items: CargoDryInventoryRow[]; total: number; page: number; pageSize: number }

export interface CargoDryMovement {
  id: number; providerProfileId: number; productCode: string; batchCode: string | null; kitId: number | null;
  movementType: number; movementTypeName: string; quantity: number; balanceAfter: number | null;
  commercialModelName: string | null; salesChannelName: string | null;
  referenceType: string | null; referenceId: number | null; note: string | null;
  createdAtUtc: string; createdByUserId: number | null;
}
export interface CargoDryMovementPaged { items: CargoDryMovement[]; total: number; page: number; pageSize: number }

export const getInventory = (params?: { productCode?: string; search?: string; hasAvailableStock?: boolean; page?: number; pageSize?: number }) => ...
export const getMovements = (params?: { productCode?: string; batchCode?: string; movementType?: number; page?: number; pageSize?: number }) => ...
```
Pass params as query string; default page=1, pageSize=25 (inventory) / 50 (movements).

## 3. hooks — `src/features/cargodry/hooks/useCargoDry.ts`
Add `useCargoDryInventory(params)` (queryKey `['cargodry','inventory',params]`) and `useCargoDryMovements(params)`
(queryKey `['cargodry','movements',params]`), same options style as the existing hooks.

## 4. Page — replace the placeholder card in `CargoDryInventoryPage.tsx`
Two stacked cards where the placeholder was:

**A. Inventory table card** ("Konsinye Stok" / "Consignment Stock")
Columns (DTO field → TR label): productCode → **Ürün**, batchCode → **Batch**, commercialModelName → **Ticari Model**,
salesChannelName → **Kanal**, totalAllocated → **Tahsis**, totalActivated → **Aktive**, availableStock → **Mevcut**
(gold-emphasized), lastMovementAtUtc → **Son Hareket** (formatted date, "—" when null). Rows are selectable; the
selected row filters the ledger below (by its productCode + batchCode) and highlights. Loading skeleton rows;
empty state "Henüz konsinye stok tahsisi yok." Right-align numeric columns.

**B. Movement ledger card** ("Stok Hareketleri" / "Stock Movements")
Columns: createdAtUtc → **Tarih**, movementTypeName → **Tür** (colored chip, mapping below), quantity → **Miktar**
(show sign, green for +, danger for −), balanceAfter → **Bakiye**, note → **Not** ("—" when null). Newest first.
When a table row is selected, header shows "{productCode} · {batchCode}" and a "Tümünü göster" reset. Paginated or
"Daha fazla" if `total > pageSize`. Empty state "Hareket kaydı yok."

Movement-type → chip tone (reuse existing severity/tone tokens):
`BatchAllocated`(1)→navy/info, `KitActivated`(2)→gold, `KitRevoked`(3)→danger, `KitReturned`(4)→muted,
`KitTransferred`(5)→navy, `ManualAdjustment`(6)→warning. Localize the label via a map keyed on `movementType`
(don't rely on the raw English `movementTypeName`).

## 5. i18n — `src/shared/i18n/locales/{tr,en}/cargodry.json`
Add an `inventory` block (table title/subtitle, column labels, empty/loading, "Tümünü göster") and a `movements`
block (title, column labels, the 6 movement-type labels keyed by enum value, empty state). Keep the existing keys.

## 6. Gate
`npm run build` / `tsc -b` clean. No console errors on the page.

## Acceptance (provider2, after CI-2-0 + CI-2a live)
- Inventory table shows 1 row: Ürün STANDARD-90, Batch 202507-CONS-PRV2, Tahsis 7, Aktive 4, Mevcut 2.
- Ledger shows 6 rows newest-first: Revoked(−1, bal 2), 4× Activated(−1, bal 5→…), Allocated(+7, bal 7) with the
  right colored chips.
- Selecting the table row filters the ledger to that product/batch; "Tümünü göster" clears it.
- KPI board + donut + alert banner (CI-1) still render above, unchanged.
- Empty state renders cleanly for a provider with no inventory.
