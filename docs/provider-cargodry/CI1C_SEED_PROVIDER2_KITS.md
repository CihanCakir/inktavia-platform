# CI-1c — seed CargoDry kits for provider2 (100011) so the provider board shows real data

Provider scoping (CI-1b) is verified and tenant-safe, but **provider2 (`ProviderProfileId = 100011`,
`provider2@inktavia.com`) owns zero CargoDry kits**, so its board is all-zero. Seed a small, realistic set of
provider-held kits for 100011 so the KPI board, kit-status donut, and a critical alert banner render — and so
CI-2 (inventory table + ledger) has data to verify against.

## Constraints
- The existing `CargoDryBatchMockSeed` is idempotent on **"any kit exists"** — it is already skipped (8 kits
  present), so **do not** extend it. Add a **separate** seeder keyed on provider 100011.
- Dev/local only, boot-safe (wrap in try/catch; never crash startup), idempotent.
- Use the entity's own methods — no raw column writes. Provider consignment uses
  `SalesChannel.ConsignmentSellThrough` (3) + `CargoDryCommercialModel.PrincipalSale` (2), matching
  `AllocateBatchToProviderCommandHandler` / the enum docs.

## New seeder: `CargoDryProviderMockSeed`
Create `src/Aizen.Modules.CargoDry.Repository/Seed/CargoDryProviderMockSeed.cs`, mirroring
`CargoDryBatchMockSeed`'s shape (ctor takes `CargoDryDbContext` + logger; `SeedAsync(CancellationToken)`).

```csharp
const long Provider2 = 100011;
if (await _db.Kits.AnyAsync(k => k.ProviderProfileId == Provider2, ct)) { /* log skip */ return; }
```

Create one batch for provider2 (unique code, e.g. `202507-CONS-PRV2`, productCode `STANDARD-90`, kitCount 7,
adminId 10001), then 7 kits — each `CargoDryKitEntity.Create(...)` then `AssignToProvider(Provider2,
SalesChannel.ConsignmentSellThrough, CargoDryCommercialModel.PrincipalSale)` — with this spread (owner/vessel ids
are arbitrary, no FK enforced; use 10021–10024 / vessels 21–24):

| Kit code | After AssignToProvider | Resulting state | Feeds |
|---|---|---|---|
| `CDK-PRV2-0001` | (nothing) | Available, provider-held | Mevcut Stok |
| `CDK-PRV2-0002` | (nothing) | Available, provider-held | Mevcut Stok |
| `CDK-PRV2-0003` | `Activate(10021, 21, 120)` | Activated, healthy | Aktif Kitler, donut Aktif |
| `CDK-PRV2-0004` | `Activate(10022, 22, 120)` | Activated, healthy | Aktif Kitler |
| `CDK-PRV2-0005` | `Activate(10023, 23, 20)`  | Activated, expiring ≤30d | Yaklaşan Yenilemeler (Warning) |
| `CDK-PRV2-0006` | `Activate(10024, 24, 5)`   | Activated, expiring ≤7d  | **Kritik Uyarı banner** (RenewalDue/Critical) |
| `CDK-PRV2-0007` | `Revoke("Provider demo revoke")` | Revoked | İade / İptal, Revoked alert |

`AddRangeAsync` all 7, `SaveChangesAsync`, log a one-line summary.

> `AssignToProvider` sets `SalesChannel` before `Activate`, so activation proceeds (won't fall into
> `CommercialReviewRequired`). Keep the calls in that order.

## Wire into boot
Register/run `CargoDryProviderMockSeed.SeedAsync` inside the existing `SeedCargoDryAsync` app extension, **after**
`CargoDryBatchMockSeed` (so both run; provider seed's own 100011 guard makes it idempotent). Resolve it from DI the
same way the batch mock seed is resolved. Boot-safe try/catch around the call.

## Acceptance
Rebuild + restart **cargodry-api** (seed runs on container start). Then for provider2:
- `GET /provider/cargodry/overview` → `availableKits: 2`, `activatedKits: 4`, `renewalDueSoonKits: 2`
  (≤30d includes the ≤7d), `revokedKits: 1`, `openOperationalAlertCount ≥ 1`; batch counters stay `0`
  (provider scope).
- `GET /provider/cargodry/alerts?take=25` → includes a Critical `RenewalDue` for `CDK-PRV2-0006` and an Info
  `Revoked` for `CDK-PRV2-0007`; every item's `providerProfileId == 100011`.
- Admin/global overview unchanged except totals grow by the 7 new kits (no cross-tenant leak — other providers
  still see their own).

## Report
Append to `REPORT_BACKEND.md` ("CI-1c"): added `CargoDryProviderMockSeed` (dev-only, idempotent on
`ProviderProfileId==100011`); 1 batch + 7 provider-held kits giving provider2 a populated board for CI-2.
