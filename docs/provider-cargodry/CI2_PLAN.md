# CI-2 — Provider CargoDry inventory table + movement ledger (plan)

Replaces the "Konsinye Stok Yönetimi" placeholder on the provider CargoDry Envanter page with a real
**inventory table** (provider's stock rows per product/batch) and a **movement ledger** (immutable stock history).

The module already has the full admin inventory stack — paged list, per-provider detail, movement ledger, DTOs —
at `api/v1/cargodry/admin/inventory` (Admin/SuperAdmin only). CI-2 is mostly **exposing the provider-scoped slice**
through the provider controller + BFF, then building the FE. No new domain model.

## Key prerequisite (must run first)
The CI-1c seed assigned kits to provider2 directly (`AssignToProvider`) — it did **NOT** create
`CargoDryProviderInventoryEntity` rows or `CargoDryInventoryMovementEntity` rows (only the
`AllocateBatchToProvider` command does). So the inventory table + ledger would be **empty** for provider2 even
though kits exist. → **CI-2-0** seeds those two tables to match the 7 kits.

## Phases
- **CI-2-0 — seed inventory + movement rows** (`CI2_0_SEED_INVENTORY.md`): extend `CargoDryProviderMockSeed` to
  also write one ProviderInventory aggregate row + the matching movement ledger for provider2, consistent with
  the 7 kits (Allocated 7, Activated 4, Revoked 1, Available 2). Prerequisite for any visible CI-2 data.
- **CI-2a — backend provider surface** (`CI2A_BACKEND_PROVIDER_INVENTORY.md`): provider-scoped inventory list +
  movements endpoints on `CargoDryProviderController` (force `ProviderProfileId` from the assertion; never trust a
  client-supplied id). BFF Refit + controller at `/provider/cargodry/inventory` and
  `/provider/cargodry/inventory/movements`. Reuses the existing queries unchanged.
- **CI-2b — frontend** (`CI2B_FRONTEND_INVENTORY.md`): inventory table + movement ledger replacing the placeholder
  card; API client + hooks + i18n + `tsc` gate.

Order: **CI-2-0 → CI-2a → CI-2b**, verifying on screen after CI-2b.
