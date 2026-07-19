# CI-2-0 — seed ProviderInventory + movement ledger for provider2 (prerequisite for CI-2)

CI-1c seeded 7 kits for provider2 (100011) via `AssignToProvider`, but did **not** create the
`CargoDryProviderInventoryEntity` aggregate row or `CargoDryInventoryMovementEntity` ledger rows (only
`AllocateBatchToProvider` does). The CI-2 inventory table + movement ledger read those two tables, so they'd be
empty. Extend `CargoDryProviderMockSeed` to also write them, consistent with the 7 kits.

## Target end-state for provider 100011
Batch `202507-CONS-PRV2`, product `STANDARD-90`, channel `ConsignmentSellThrough`, model `PrincipalSale`:
- TotalAllocated **7**, TotalActivated **4**, TotalRevoked **1**, TotalReturned 0, TotalAdjusted 0 →
  AvailableStock **2** (matches the 2 Available kits).

## Extend `CargoDryProviderMockSeed.SeedAsync` (same idempotency guard, dev-only)
Inject `ICargoDryProviderInventoryRepository` + `ICargoDryInventoryMovementRepository` into the seeder. Guard on
the same 100011 kit check already present (so the whole block is skipped once seeded). After the kits are added:

1. **Inventory row** — `CargoDryProviderInventoryEntity.Create(100011, "STANDARD-90", "202507-CONS-PRV2",
   CargoDryCommercialModel.PrincipalSale, SalesChannel.ConsignmentSellThrough,
   StockLocationType.ProviderWarehouse, initialAllocated: 7, nowUtc)`, then apply
   `IncrementActivated(4, nowUtc)` and `IncrementRevoked(1, nowUtc)` so AvailableStock computes to 2. `AddAsync`.

2. **Movement ledger** — create rows with a running `balanceAfter` (available pool after each), all for
   (100011, "STANDARD-90", batch "202507-CONS-PRV2"), using `CargoDryInventoryMovementEntity.Create(...)`:

   | # | MovementType | quantity | balanceAfter | note |
   |---|---|---|---|---|
   | 1 | `BatchAllocated` | +7 | 7 | "Batch allocated to provider (demo seed)" |
   | 2 | `KitActivated` | -1 | 6 | kitId of CDK-PRV2-0003 |
   | 3 | `KitActivated` | -1 | 5 | CDK-PRV2-0004 |
   | 4 | `KitActivated` | -1 | 4 | CDK-PRV2-0005 |
   | 5 | `KitActivated` | -1 | 3 | CDK-PRV2-0006 |
   | 6 | `KitRevoked`  | -1 | 2 | CDK-PRV2-0007, note "Provider demo revoke" |

   Pass `commercialModel: PrincipalSale`, `salesChannel: ConsignmentSellThrough` on each; `kitId` where a kit
   applies (rows 2-6 — use the entity ids after the kits are saved, or leave null if ids aren't easily available
   at seed time). Stagger `CreatedAtUtc` a few minutes apart so the ledger sorts naturally. `AddRangeAsync`,
   `SaveChangesAsync`. Keep it boot-safe (try/catch) — a seed failure must never crash startup.

> AvailableStock is computed (`TotalAllocated + TotalAdjusted − TotalActivated − TotalRevoked − TotalReturned`),
> so do not set it directly; the Increment* calls drive it to 2.

## Acceptance
Rebuild + restart **cargodry-api** (seed runs on boot). Then (admin or, after CI-2a, provider):
- Inventory list for 100011 → 1 row: STANDARD-90 / 202507-CONS-PRV2, Allocated 7, Activated 4, Available 2.
- Movements for 100011 → 6 rows, newest first, balances 7→6→5→4→3→2, types BatchAllocated ×1, KitActivated ×4,
  KitRevoked ×1.
- Idempotent: restarting again does not duplicate rows.

## Report
Append to `REPORT_BACKEND.md` ("CI-2-0"): `CargoDryProviderMockSeed` now also seeds the ProviderInventory row +
6-row movement ledger for provider2 so the CI-2 table/ledger render.
