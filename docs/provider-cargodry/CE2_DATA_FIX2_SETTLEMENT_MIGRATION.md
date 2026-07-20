# CE-2 data FIX 2 — schema drift on sell_through_settlements (missing InvoiceId + payout columns)

The seed now fails on the real DB error:
```
42703: column "InvoiceId" of relation "sell_through_settlements" does not exist
```
This is **schema drift**, not a seed bug: `CargoDrySellThroughSettlementEntity` was extended with invoice/payout
lifecycle fields (`InvoiceId`, `PayoutRecordId`, `InvoicePreparedAtUtc/ByUserId/Note`,
`PaymentPreparedAtUtc/ByUserId/Note`, `PayoutCompletedAtUtc/ByUserId`, `PayoutCompletionReference`,
`PayoutFailureReason`, `PayoutLifecycleNote`, `ReadyForSettlementAtUtc`, …) but **no EF migration was ever generated**
to add those columns. Any `INSERT` into `sell_through_settlements` (including admin settlement flows) fails — the seed
just surfaced it first. Fix the schema, not the seed.

## Fix — generate + apply the missing migration
1. Diff the entity against the DB and generate the migration for the CargoDry context:
   ```
   dotnet ef migrations add SyncSellThroughSettlementColumns \
     -p Modules/CargoDry/src/Aizen.Modules.CargoDry.Repository \
     -s Modules/CargoDry/src/Aizen.Modules.CargoDry \
     -c CargoDryDbContext
   ```
   (Use the project's actual startup/context wiring; EF will emit `AddColumn` for **every** entity property that has
   no column — `InvoiceId` and the other payout/invoice lifecycle fields.)
2. Review the generated migration: it should only `AddColumn` the missing settlement fields (nullable, no data loss),
   nothing destructive. Include the `.Designer.cs` + snapshot update.
3. Ensure it is applied on boot (the CargoDry module runs migrations at startup via `AddAizenUnitOfWork`; a fresh
   container start will apply it). If migrations are not auto-applied, run `dotnet ef database update` for the context.

## Also verify the sibling tables
While here, confirm `cargo_dry_sales_attributions` (and any other recently-extended CargoDry table) is in sync — the
attribution insert didn't error, but re-run a quick check that the model snapshot has no other pending changes:
```
dotnet ef migrations has-pending-model-changes -c CargoDryDbContext   # or `migrations add` shows "no changes"
```
If the attribution table also has drift, include it in the same migration.

## Acceptance
Rebuild + `up -d --force-recreate cargodry-api`:
- Boot log shows the migration applied and **"Seeded CargoDry provider2 sales attributions (3) + settlement (1)."**
  with **no** "failed (non-fatal)".
- `GET /provider/cargodry/earnings` (provider2): `thisMonthCommission` = 90, `ytdCommission` = 90,
  `avgEarningPerKit` ≈ 22.5, `pendingPayout` = 60.
- Admin settlement create/list still work (schema now matches the entity).

## Report
`REPORT_BACKEND.md` ("CE-2 data fix 2"): added `SyncSellThroughSettlementColumns` migration to add the missing
invoice/payout lifecycle columns to `sell_through_settlements` (pre-existing schema drift surfaced by the earnings
seed); settlement inserts now succeed.
