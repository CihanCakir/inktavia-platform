using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.CargoDry.Repository.Migrations
{
    /// <summary>
    /// Repairs schema drift on long-lived databases where two earlier CargoDry migrations
    /// (<c>AddCargoDrySettlementInvoicePreparationFields</c> and
    /// <c>AddCargoDrySalesAttributionTierSnapshot</c>) are recorded in <c>__EFMigrationsHistory</c>
    /// but their <c>ALTER TABLE ... ADD COLUMN</c> operations were never effected — leaving the
    /// entity model reading columns the table does not have.
    ///
    /// Symptom: <c>GET /commercial/settlements</c> and <c>GET /commercial/sales-attributions</c>
    /// returned HTTP 500 with
    /// <c>Npgsql.PostgresException 42703: column "InvoiceId" does not exist</c> (settlements) and
    /// <c>... column "TierAtSale" does not exist</c> (sales attributions), because the paged
    /// queries materialise the full entity and PostgreSQL rejects the SELECT — even for an empty
    /// table. The module's startup migrator cannot self-heal this: it only runs when
    /// <c>GetPendingMigrationsAsync()</c> is non-empty, and every MigrationId is already recorded.
    ///
    /// This migration re-applies exactly those column (and index) additions idempotently via
    /// <c>ADD COLUMN IF NOT EXISTS</c> / <c>CREATE INDEX IF NOT EXISTS</c>, so it repairs drifted
    /// databases and is a safe no-op on correctly-migrated ones (fresh installs already have the
    /// columns from the original migrations). The model is unchanged, so there is no snapshot diff.
    /// </summary>
    public partial class ReconcileCargoDryCommercialSchemaDrift : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── sell_through_settlements: Phase 4C invoice-preparation columns ────────────
            migrationBuilder.Sql(
                "ALTER TABLE cargodry.sell_through_settlements " +
                "ADD COLUMN IF NOT EXISTS \"InvoiceId\" bigint NULL;");
            migrationBuilder.Sql(
                "ALTER TABLE cargodry.sell_through_settlements " +
                "ADD COLUMN IF NOT EXISTS \"InvoicePreparationNote\" character varying(1000) NULL;");
            migrationBuilder.Sql(
                "ALTER TABLE cargodry.sell_through_settlements " +
                "ADD COLUMN IF NOT EXISTS \"InvoicePreparedAtUtc\" timestamp with time zone NULL;");
            migrationBuilder.Sql(
                "ALTER TABLE cargodry.sell_through_settlements " +
                "ADD COLUMN IF NOT EXISTS \"InvoicePreparedByUserId\" bigint NULL;");
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_sell_through_settlements_InvoiceId\" " +
                "ON cargodry.sell_through_settlements (\"InvoiceId\") " +
                "WHERE \"InvoiceId\" IS NOT NULL;");

            // ── sales_attributions: tier-at-sale snapshot columns ────────────────────────
            migrationBuilder.Sql(
                "ALTER TABLE cargodry.sales_attributions " +
                "ADD COLUMN IF NOT EXISTS \"TierAtSale\" character varying(20) NULL;");
            migrationBuilder.Sql(
                "ALTER TABLE cargodry.sales_attributions " +
                "ADD COLUMN IF NOT EXISTS \"TierBonusRate\" numeric(8,4) NOT NULL DEFAULT 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: the reconciled columns are owned by the original migrations
            // (AddCargoDrySettlementInvoicePreparationFields / AddCargoDrySalesAttributionTierSnapshot).
            // Dropping them here would double-drop and re-introduce the very drift this repairs.
        }
    }
}
