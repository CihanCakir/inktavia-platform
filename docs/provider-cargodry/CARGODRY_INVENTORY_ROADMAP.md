# CargoDry Komuta Merkezi (Provider Inventory Dashboard) — Backend + Frontend Roadmap

The approved Stitch design is a rich **"Komuta Merkezi"**: a critical alert banner, a 5-card KPI board, a weekly renewal
calendar (stacked bar), a kit-status donut, the consignment-stock table, and the inventory movement ledger. This roadmap
phases it against the **existing CargoDry module** (which is very mature) — the key gap is that **no provider BFF exists
for CargoDry yet**; almost every panel is backed by a module query that just needs provider-scoped BFF wiring, done the
same way as the Jobs BFF.

Route: `/app/cargodry-inventory` (already in the provider nav, currently YAKINDA). The design's own left rail ("Kaptan
Paneli": Operasyonlar / Envanter Kontrol / Cihaz Sağlığı / …) is a Stitch mock — **the content lives inside the existing
Inktavia provider shell**, exactly like the Jobs dashboard. We adapt the panels, not the chrome.

## What EXISTS today (reuse — do NOT rebuild)
Module queries (all take/allow a `ProviderProfileId` filter — the BFF scopes them to the caller):
- **`GetCargoDryOperationalOverview`** → the KPI board **and** the kit-status donut in one shot:
  `TotalKits, AvailableKits, ActivatedKits, ExpiredKits, RevokedKits, RenewalDueSoonKits (≤30d),
  ProviderHeldKits, WarehouseStockKits, RecentLifecycleEventCount, OpenOperationalAlertCount`.
- **`GetCargoDryOperationalAlerts`** → the **critical alert banner** + the "Kritik Uyarılar" KPI. Alert DTO:
  `AlertType (ExpiringSoon | Expired | Revoked | CommercialReviewRequired | RenewalDue), Severity, Message,
  DaysUntilExpiry, ExpiresAt, KitCode/ProductName/VesselName`.
- **`GetProviderInventoryList`** (paged + filters) → the **Konsinye Stok Yönetimi** table.
- **`GetProviderInventoryMovements`** (paged) → the **Envanter Hareket Kayıtları** ledger (signed qty + BalanceAfter).
- **`GetProviderInventoryDetail`** → rolled-up stock totals (backup for the stock KPI).
- **`GetCargoDryRenewalCandidates`** → the **Yenileme Takvimi** chart + the "Yaklaşan Yenilemeler" KPI. Candidate DTO:
  `KitId, ProductCode/Name, Status, ExpiresAtUtc, DaysUntilExpiry, RenewalPrice, CanPrepareRenewal, BlockingReasons`.
- **`GetMyKits`, `GetCargoDryKitDetail`, `GetCargoDryKitLifecycleHistory`** → kit drill-downs (later).

Commands for the header actions (already exist):
- **`AllocateBatchToProvider`** → **"Stok Girişi"** (receive stock into provider inventory).
- **`ActivateKit`** → **"Kit Aktive Et"**.
- **`RenewKit` / `PrepareCargoDryKitRenewal` / `CompleteCargoDryRenewal`** → renewals.
- `RevokeKit, TransferKit, AdjustProviderInventory, ValidateKit` → secondary ops.

## What is MISSING / different (build, or defer honestly)
| Design element | Reality | Action |
|---|---|---|
| **Provider BFF for CargoDry** | none (only AdminPanel BFF) | **build** — CI-1..CI-4 (mirror the Jobs BFF: assertion identity, scope to `ProviderProfileId`) |
| KPI board + kit-status donut | `GetCargoDryOperationalOverview` covers it | wire only |
| Critical alert banner + "Kritik Uyarılar" | `GetCargoDryOperationalAlerts` (operational, NOT telemetry) | wire; **relabel** — see flags |
| Consignment table + movement ledger | inventory queries exist | wire only |
| Yenileme Takvimi (weekly stacked bar) | `GetCargoDryRenewalCandidates` gives per-kit expiry | **bucket by week × product-family** — small BFF/FE aggregation |
| **"Sensör Hatası" / Cihaz Sağlığı / humidity %65 alert** | **telemetry/device module does not exist** (project: "future device/telemetry") | **defer** — do NOT fabricate sensor data; the alert card uses the real operational alerts instead |
| **"Acil Durum Bildir"** (emergency/incident) | no incident command | **defer / hide** (post-MVP, tied to telemetry) |
| Design's own left rail ("Kaptan Paneli") | Stitch mock | render content inside the Inktavia provider shell |
| Owner/vessel names on alerts/renewals | the DTOs carry `OwnerDisplayName/VesselName` | **decision needed** — for CargoDry consignment the provider likely MAY see the vessel/owner of a kit they sold (unlike anonymous SR discovery); confirm before showing |

---

## Backend roadmap (provider-scoped; every query filtered to the caller's `ProviderProfileId`)

### CI-1 — Provider CargoDry BFF foundation + Overview/Alerts  ⭐ (unblocks the page)
Create the provider BFF surface (new controller `ProviderCargoDryController` on MarineProvider BFF + a
`IProviderCargoDryRemoteCall` Refit to the module), provider-scoped via the assertion (`ProviderProfileId` from the
holder, never client-sent), mirroring `ProviderJobsController`.
- `GET /provider/cargodry/overview` → `GetCargoDryOperationalOverview(providerProfileId)` → KPI board + donut source.
- `GET /provider/cargodry/alerts?take=n` → `GetCargoDryOperationalAlerts(providerProfileId)` → banner + "Kritik Uyarılar".
- Confirm both module queries accept/apply a provider filter; if a query is admin-global, add a `ProviderProfileId`
  parameter (the DTOs already carry the field).

### CI-2 — Inventory list + movements (the table + ledger)
- `GET /provider/cargodry/inventory` (paged + `productCode/commercialModel/salesChannel` filters) →
  `GetProviderInventoryList`.
- `GET /provider/cargodry/inventory/movements` (paged) → `GetProviderInventoryMovements`.
- (Optional) `GET /provider/cargodry/inventory/detail` → `GetProviderInventoryDetail` (aggregate fallback).

### CI-3 — Renewals: candidates + weekly calendar buckets
- `GET /provider/cargodry/renewals/candidates` → `GetCargoDryRenewalCandidates(providerProfileId)` (the "Yaklaşan
  Yenilemeler" KPI = count with `DaysUntilExpiry ≤ 14`).
- Weekly chart: either (a) a BFF aggregation returning `{ weekStart, productFamily, count }[]` over the next 6 weeks
  from the candidates, or (b) return the raw candidates and let the SPA bucket them. Prefer (a) to keep the SPA thin —
  a small `GET /provider/cargodry/renewals/calendar?weeks=6` that groups candidates by ISO-week × product family
  (Premium vs Sense, from ProductCode).

### CI-4 — Header-action command endpoints (expose existing commands)
- `POST /provider/cargodry/inventory/stock-entry` → `AllocateBatchToProvider` (guarded: provider owns the target
  inventory; validate batch eligibility via `GetBatchAllocationPreview`).
- `POST /provider/cargodry/kits/activate` → `ActivateKit`.
- `POST /provider/cargodry/kits/{kitId}/renew` → `PrepareCargoDryKitRenewal` / `RenewKit`.
- Each: ownership + state guards in the handler (mirror the JD-2 pattern), idempotency where the command implies it.
- **Do NOT** add telemetry/incident endpoints — no domain for them yet.

---

## Frontend roadmap (build against CI-1..CI-4; reuse Jobs-dashboard components)

- **FE-A — Shell + KPI board.** Page inside the provider shell; header ("CargoDry Envanter", subtitle) + secondary
  actions (Stok Girişi, Kit Aktive Et). 5 KPI cards from `overview` (Mevcut Stok — gold-emphasized; Aktif Kitler;
  Yaklaşan Yenilemeler; Kritik Uyarılar; İade/İptal). Reuse the Jobs KPI card component.
- **FE-B — Critical alert banner.** Top banner from `alerts` (highest-severity item): icon + message + "Detaya Git".
  Severity → color (error/amber/navy). Hidden when no open alerts. **No fake sensor/humidity text** — real alert messages
  only.
- **FE-C — Charts (self-contained SVG, like JobsAnalytics).** Yenileme Takvimi stacked bar from `renewals/calendar`
  (weeks × Premium/Sense); Kit Durumu donut from `overview` (Aktif = ActivatedKits, Yenileme Yaklaşıyor =
  RenewalDueSoonKits, Gecikmiş = ExpiredKits, Kurulum Bekliyor = AvailableKits). Reuse the SVG bar/donut from
  `JobsAnalytics.tsx` (recharts is 403 in the sandbox — SVG only).
- **FE-D — Consignment table.** From `inventory` (paged): Ürün (+ batch subline), Ticari Model (chip), Satış Kanalı
  (chip), Tahsis, Aktive, Mevcut (bold; amber "Kritik" pill on low stock), Son Hareket. Filtrele + "Excel İndir"
  (client CSV export). Reuse the Jobs enriched-table styling.
- **FE-E — Movement ledger.** From `inventory/movements`: type badge + localized name, signed quantity (green +/navy −),
  Ürün · Batch, "Bakiye: N", note, relative date; "Daha fazla".
- **FE-F — Header actions (modals).** Stok Girişi (batch/serial + qty → CI-4 stock-entry, with the eligibility preview),
  Kit Aktive Et (serial/QR → CI-4 activate). Success → invalidate overview + inventory + movements.
- **FE-G — States + i18n.** Loading skeletons, empty ("Henüz konsinye stok yok…"), error/not-linked; localize every
  enum (CommercialModel, SalesChannel, MovementType, AlertType, KitStatus) — never render raw codes. TR/EN.

## Suggested execution order (highest value first)
1. **CI-1 + FE-A/FE-B** — the page becomes real: KPI board + kit-status source + the alert banner.
2. **CI-2 + FE-D/FE-E** — the consignment table + movement ledger (the operational core).
3. **CI-3 + FE-C** — renewal calendar + kit-status donut charts.
4. **CI-4 + FE-F** — Stok Girişi / Kit Aktive Et actions.

## Reality flags to keep (do not let the UI over-promise)
- **No telemetry / device health / humidity sensors** — the project marks these "future". The alert banner + "Kritik
  Uyarılar" use the **real operational alerts** (expiry/renewal/commercial), not fabricated sensor readings. The design's
  "Cihaz Sağlığı" nav item and "Sensör Hatası" label are **relabeled/deferred**.
- **"Acil Durum Bildir"** → deferred (no incident domain).
- **Design's left rail is a mock** → content renders inside the Inktavia provider shell.
- **Owner/vessel visibility** on alerts/renewals is a product decision (CargoDry consignment likely permits it; SR
  discovery did not) — confirm before rendering `OwnerDisplayName/VesselName`.
- Quantities are integer kit counts, not currency. Provider-scoped everywhere (assertion identity, never client-sent).
