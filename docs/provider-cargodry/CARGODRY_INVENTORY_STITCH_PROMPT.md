# CargoDry — Envanter (Provider Inventory Dashboard) — Stitch Design Spec

Provider portal screen (`/app/cargodry-inventory`). The provider (a marine service business) holds **CargoDry consignment
stock** — kits allocated from platform batches that they resell/activate for customers. This dashboard is their stock
control: KPIs, per-product/batch inventory, and the movement ledger. Grounded in the **existing CargoDry module**
(provider-inventory queries + movement ledger) — the "Data" notes say what already exists vs what still needs wiring.

---

## 0. Data Reality Notes (the design must not over-promise)
- **Backend module exists and is rich** (`Modules/CargoDry`): `GetProviderInventoryList` (paged), `GetProviderInventoryDetail`
  (aggregate totals + rows), `GetProviderInventoryMovements` (paged ledger). DTOs verified: list item, full row, detail,
  movement.
- **What's MISSING:** there is **no provider BFF** for CargoDry yet (only an AdminPanel BFF exists). So the SPA needs a new
  provider BFF surface (`GET /provider/cargodry/inventory`, `.../inventory/detail`, `.../inventory/movements`) wired the
  same way as the Jobs BFF (provider identity via the assertion, scoped to the caller's `ProviderProfileId`). → backend
  prompt after the design.
- **Kit activation / renewals / settlement are separate screens** — this dashboard links to them but does not own them.
- **No customer identity** shown (privacy). Money/settlement is a different screen; this one is stock-only.

### Real fields (design against these)
- **Aggregate (detail):** `TotalAllocated, TotalActivated, TotalRevoked, TotalReturned, TotalAdjusted, TotalAvailable,
  LastMovementAtUtc`.
- **Per-row (list item):** `ProductCode, BatchCode?, CommercialModelName, SalesChannelName, TotalAllocated,
  TotalActivated, AvailableStock, LastMovementAtUtc`.
- **Movement (ledger):** `MovementTypeName, Quantity (+/−), ProductCode, BatchCode?, KitId?, BalanceAfter?, Note?,
  CreatedAtUtc`.
- **Enum vocab (localize; never render raw codes):**
  - CommercialModel: MarketplaceCommission · PrincipalSale · SubscriptionBilling
  - SalesChannel: DirectSale · ProviderResale · ConsignmentSellThrough · ProviderAttributedSale
  - StockLocationType: PlatformWarehouse · ProviderWarehouse · Transit · Activated
  - MovementType: BatchAllocated (+) · KitActivated (−) · KitRevoked · KitReturned · KitTransferred · ManualAdjustment

---

## 1. Layout Hierarchy
Inside the Inktavia provider shell (same sidebar/topbar). Full-width page:
1. **Page Header** — title "CargoDry Envanter", subtitle ("Konsinye stoklarınızı buradan yönetin."), right-aligned
   secondary actions: **"Kit Aktive Et"** (→ activation screen) and **"Hareketler"** (scroll/toggle to the ledger).
2. **KPI Board (4 cards)** — from the aggregate: **Mevcut Stok** (TotalAvailable), **Aktive Edilen** (TotalActivated),
   **Tahsis Edilen** (TotalAllocated), **İade / İptal** (TotalReturned + TotalRevoked). Each: value + small label +
   subtle icon; the "Mevcut Stok" card is emphasized (gold accent). A **low-stock** signal if available is at/below a
   threshold.
3. **Filters row** — search (product), and dropdown filters: Ticari Model, Satış Kanalı. A "Yenile" refresh.
4. **Inventory table** — one row per product/batch (paginated). Columns: **Ürün** (product + batch code subline), **Ticari
   Model** (chip), **Satış Kanalı** (chip), **Tahsis** (allocated), **Aktive** (activated), **Mevcut** (available, bold;
   low-stock in amber/danger), **Son Hareket** (relative date). Row → inventory detail (future) / expands movements.
5. **Movement Ledger** (below, or a right drawer / second tab "Hareketler") — recent movements: type icon + name,
   signed quantity (green +, navy −), product/batch, BalanceAfter, note, date. Paginated "Daha fazla".

## 2. Component Details
### A. KPI cards
Soft-filled cards (bg surface, navy text). "Mevcut Stok" gets the gold emphasis (like the Jobs "Aktif İşler" card).
Numbers are large (font-display). Under each, a one-line muted descriptor.

### B. Inventory table
Clean table, navy headers, hover row. **Type chips**: CommercialModel and SalesChannel as soft chips (navy/gold/neutral).
**Mevcut** column: bold; if `AvailableStock <= lowThreshold` → amber pill "Düşük". Empty → "Henüz konsinye stok yok."

### C. Movement ledger
Chronological list (newest first). Each row: a movement-type glyph in a circular badge (allocated = down-into-store gold,
activated = check navy, returned/revoked = muted, transfer = arrows, adjustment = pencil), the localized type name, the
signed quantity, `Ürün · Batch`, `Bakiye: {BalanceAfter}`, optional note, and a relative timestamp. This is the audit
trail — read-only.

### D. Header actions
- **"Kit Aktive Et"** (gold) → the activation flow (separate screen; a shortcut here).
- **"Hareketler"** (secondary) → jumps to / opens the ledger.

## 3. Technical Design Rules (Nautical Heritage — same as every other provider screen)
- Colors: canvas `#F9F8F6` · surface `#F2EEE9` · card `#FFFFFF` · navy `#002147` · gold `#C5A059` · danger `#8B0000` ·
  amber for low-stock warnings.
- Type: headings `EB Garamond`; UI/data `Hanken Grotesk`. Radius: card 8px · chip/button 4px.
- Chips soft-filled; KPI board mirrors the Jobs dashboard KPIs; tables match the Jobs enriched table styling.
- Quantities are integers (kits), not currency. Dates relative ("2 gün önce") with exact on hover.

## 4. States
- **Loading** skeletons for KPIs + table.
- **Empty**: no inventory yet ("Henüz konsinye stok yok. Platform bir batch tahsis ettiğinde burada görünecek.").
- **Error / not-linked** (provider profile not linked): friendly error.
- **Low stock**: amber pill on the row + optional KPI hint.

## 5. Claude / Stitch Master Prompt (EN — directly usable)

> "Design a 'CargoDry Inventory' dashboard for the Inktavia Marine OS provider portal using its Nautical Heritage design
> system. A full-width page inside the provider shell: a header ('CargoDry Envanter', subtitle 'Manage your consignment
> stock', with a gold 'Activate Kit' button and a secondary 'Movements' button on the right). Below, a KPI board of four
> soft-filled cards — Available Stock (emphasized, gold accent), Activated, Allocated, Returned/Revoked — each with a
> large number, a small label and a subtle icon. Then a filters row (search by product, dropdowns for Commercial Model
> and Sales Channel, a refresh). Then an inventory table, one row per product/batch: Product (with a batch-code subline),
> Commercial Model (soft chip), Sales Channel (soft chip), Allocated, Activated, Available (bold; an amber 'Low' pill when
> stock is low), and Last Movement (relative date). Below the table, a Movement Ledger: a chronological read-only list of
> stock movements, each with a circular movement-type badge (batch-allocated, kit-activated, returned, transferred,
> adjustment), a localized type name, a signed quantity (green + / navy −), 'Product · Batch', a 'Balance: N' note, an
> optional note, and a relative timestamp, with a 'Load more'. Include loading skeletons and an empty state ('No
> consignment stock yet'). Colors: #F9F8F6 background, #F2EEE9 surface, #002147 navy, #C5A059 gold, amber for low-stock;
> fonts EB Garamond (headings) and Hanken Grotesk (UI); card radius 8px, control radius 4px. Quantities are integer kit
> counts, not currency. Calm, operational, roomy — matching the Jobs dashboard."

---

## 6. After the design (info — not a design decision)
- **Backend needed:** a **provider BFF** for CargoDry — `GET /provider/cargodry/inventory` (list, paged + filters),
  `GET /provider/cargodry/inventory/detail` (aggregate KPIs), `GET /provider/cargodry/inventory/movements` (ledger,
  paged). Provider-scoped via the assertion (the module queries already take `ProviderProfileId`); mirror the Jobs BFF
  pattern. → I'll write the backend prompt after you produce the Stitch design.
- **Routes:** `/app/cargodry-inventory` already exists in the provider nav (currently YAKINDA). Kit activation / renewals
  / settlement are separate screens linked from here.
