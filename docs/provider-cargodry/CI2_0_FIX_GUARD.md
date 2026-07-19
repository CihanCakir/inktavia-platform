# CI-2-0 FIX — split the seed idempotency guard (kits vs inventory)

The inventory/movement seeding code is correct but **will never run on the current DB**. The guard at the top of
`CargoDryProviderMockSeed.SeedAsync` returns early when provider2 kits already exist:
```csharp
if (await _db.Kits.AnyAsync(k => k.ProviderProfileId == Provider2, ct)) return;   // ← short-circuits
```
Provider2 kits were already seeded in the previous run (CI-1c), so on the next restart the method returns before
reaching the new inventory + movement block. Result: the inventory table + ledger stay empty.

## Fix — guard each block independently (idempotent, completes partial prior seeds)
Restructure so the kit block and the inventory/movement block each have their own existence check:

```csharp
var nowUtc = DateTime.UtcNow;

// ── Kits (skip if already seeded) ────────────────────────────────────────
if (!await _db.Kits.AnyAsync(k => k.ProviderProfileId == Provider2, ct))
{
    // ... existing batch + 7 kits + AssignToProvider + Activate/Revoke ...
    _db.Batches.Add(batch);
    await _db.Kits.AddRangeAsync(kits, ct);
    await _db.SaveChangesAsync(ct);
}

// ── Inventory row + movement ledger (skip if already seeded) ──────────────
if (!await _db.ProviderInventories.AnyAsync(pi => pi.ProviderProfileId == Provider2, ct))
{
    const string BatchCode = "202507-CONS-PRV2";   // fixed code (batch var may be out of scope now)
    var inventory = CargoDryProviderInventoryEntity.Create(
        Provider2, "STANDARD-90", BatchCode,
        CargoDryCommercialModel.PrincipalSale, SalesChannel.ConsignmentSellThrough,
        StockLocationType.ProviderWarehouse, 7, nowUtc);
    inventory.IncrementActivated(4, nowUtc);
    inventory.IncrementRevoked(1, nowUtc);
    _db.ProviderInventories.Add(inventory);

    // ... the same 6 movement rows, using the BatchCode constant ...
    await _db.InventoryMovements.AddRangeAsync(movements, ct);
    await _db.SaveChangesAsync(ct);
}
```
Keep the outer try/catch (boot-safe). Because the two blocks are independently guarded, the already-seeded kits
are left alone and the missing inventory + movements get created on this restart. Next restart both guards trip →
no duplicates.

## Acceptance
Rebuild + restart **cargodry-api**. Check logs for the seed line, then (admin inventory endpoint, or provider
after CI-2a):
- `ProviderInventories` has 1 row for 100011 (Allocated 7 / Activated 4 / Available 2).
- `InventoryMovements` has 6 rows for 100011 (balances 7→6→5→4→3→2).
- Restart again → still 1 inventory row + 6 movements (no duplication).
