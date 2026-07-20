# CE-2 data FIX 3 — real cause: DB migration drift on sell_through_settlements (not a new migration)

FIX 2 was wrong: the generated `SyncSellThroughSettlementColumns` migration is **empty** (`Up()` has no body) because
the EF model snapshot already contains `InvoiceId` + the payout columns. The actual problem is **database drift**:
- `sell_through_settlements` in the running Postgres is **missing `InvoiceId`** (error `42703`), even though the
  migration that adds it — `20260703102745_AddCargoDryCommercialFoundationFields` (`AddColumn "InvoiceId"`) — is part
  of the model and the module **auto-migrates on boot** (`Aizen.Modules.CargoDry.Repository/DependencyInjection.cs`).
- So the DB's `__EFMigrationsHistory` and the actual table columns are inconsistent: history thinks the settlement
  columns exist, the table doesn't have them. A fresh `migrations add` can't fix this (no model diff).

Delete the no-op `20260720061755_SyncSellThroughSettlementColumns` migration (+ its Designer + revert the snapshot
change if any) — it does nothing.

## Objective
Make the running DB schema match the model for the CargoDry tables (specifically `sell_through_settlements` must have
`InvoiceId` and the other payout/invoice lifecycle columns), so settlement inserts (and the earnings seed) succeed.

## Diagnose then fix (in your env — you have psql + dotnet ef)
1. **Confirm the drift:**
   ```
   docker compose exec -T postgres psql -U <user> -d <db> -c "\d sell_through_settlements"     # list columns
   docker compose exec -T postgres psql -U <user> -d <db> -c "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" LIKE '%Commercial%' OR \"MigrationId\" LIKE '%Settlement%' ORDER BY 1;"
   ```
   Compare: is `20260703102745_AddCargoDryCommercialFoundationFields` in history? Are `InvoiceId`,
   `PayoutRecordId`, `PaymentPrepared*`, `InvoicePrepared*`, `PayoutCompleted*` columns actually present?

2. **If the migration is NOT in history** → apply it:
   ```
   dotnet ef database update -c CargoDryDbContext \
     -p Modules/CargoDry/src/Aizen.Modules.CargoDry.Repository \
     -s Modules/CargoDry/src/Aizen.Modules.CargoDry
   ```

3. **If history says applied but columns are missing (true drift)** → reset the CargoDry tables (dev DB; **all
   CargoDry data is re-seeded on boot** — products, kits, inventory, movements, attributions, settlement, agreement):
   - Drop the CargoDry-owned tables (cargo_dry_*, sell_through_settlements, provider inventories/movements, kits,
     batches, products, agreements, attributions, renewals, settlement runs, stock requests, etc.) **and** remove
     their rows from `__EFMigrationsHistory`.
   - Restart cargodry-api → auto-migrate recreates every CargoDry table from scratch with the correct schema →
     seeders repopulate (including the CE-2 earnings seed).
   - (Prefer this over hand-`ALTER TABLE` — several columns are missing and a clean re-migrate is less error-prone.)

## Verify — you MUST run these after the fix and paste the output (do not report done until all pass)
Real table names: `sales_attributions`, `sell_through_settlements`, `consignment_agreements`. Run against the running
container (use the compose Postgres creds/db from `docker-compose.yaml`):

1. **Schema** — the missing column now exists:
   ```
   docker compose exec -T postgres psql -U <user> -d <db> -c "\d sell_through_settlements" | grep -i "InvoiceId\|Payout\|InvoicePrepared"
   ```
   Expect `InvoiceId` + the payout/invoice lifecycle columns listed.

2. **Boot log** — seed ran, no failure:
   ```
   docker compose logs cargodry-api | grep -iE "provider2 sales attributions|failed \(non-fatal\)"
   ```
   Expect **"Seeded CargoDry provider2 sales attributions (3) + settlement (1)."** and **no** "failed (non-fatal)".

3. **Data (no auth token needed)** — the seed persisted with the right amounts:
   ```
   docker compose exec -T postgres psql -U <user> -d <db> -c "
     SELECT count(*) AS attr_count, COALESCE(SUM(\"ProviderShareAmount\"),0) AS commission
     FROM sales_attributions WHERE \"ProviderProfileId\"=100011 AND \"Status\"<>5;
     SELECT \"ProviderPayoutAmount\", \"Status\" FROM sell_through_settlements WHERE \"ProviderProfileId\"=100011;
     SELECT count(*) FROM consignment_agreements WHERE \"ProviderProfileId\"=100011;"
   ```
   Expect: `attr_count = 3`, `commission = 90`; settlement `ProviderPayoutAmount = 60` with a Pending status
   (Status = 1); `consignment_agreements` count ≥ 1.

4. **HTTP smoke (best-effort)** — if you can obtain a provider2 token, `GET /api/v1/provider/cargodry/earnings`
   should return `thisMonthCommission=90, ytdCommission=90, pendingPayout=60, avgEarningPerKit≈22.5`. If no token is
   handy, steps 1-3 are sufficient proof at the data layer; the frontend/HTTP smoke is verified separately on screen.

If step 2 still shows "failed (non-fatal)", paste the full inner-exception message — it names the next missing column
and we iterate.

## Acceptance
Steps 1-3 pass (InvoiceId present · seed line + no failure · 3 attributions summing 90 + settlement payout 60).
Admin settlement flows work (schema now matches the model).

## Report
`REPORT_BACKEND.md` ("CE-2 data fix 3"): removed the no-op Sync migration; resolved the `sell_through_settlements`
schema drift (missing InvoiceId + payout columns) by re-applying/re-migrating the CargoDry schema; earnings seed now
persists.
