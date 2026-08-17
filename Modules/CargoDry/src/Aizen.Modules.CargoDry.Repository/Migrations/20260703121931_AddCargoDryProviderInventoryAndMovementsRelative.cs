using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Aizen.Modules.CargoDry.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddCargoDryProviderInventoryAndMovementsRelative : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ConsignmentAgreementId",
                schema: "cargodry",
                table: "kits",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "sales_attributions",
                schema: "cargodry",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KitId = table.Column<long>(type: "bigint", nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    KitCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProductCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BatchCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProviderProfileId = table.Column<long>(type: "bigint", nullable: true),
                    SalesChannel = table.Column<int>(type: "integer", nullable: false),
                    CommercialModel = table.Column<int>(type: "integer", nullable: false),
                    ConsignmentAgreementId = table.Column<long>(type: "bigint", nullable: true),
                    InventoryId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SalePrice = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    CommissionRate = table.Column<decimal>(type: "numeric(8,4)", nullable: true),
                    CommissionAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    SellThroughSettlementId = table.Column<long>(type: "bigint", nullable: true),
                    AttributedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AttributedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    ReviewNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReviewedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifyHost = table.Column<string>(type: "text", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    CreateHost = table.Column<string>(type: "text", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales_attributions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sell_through_settlements",
                schema: "cargodry",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SettlementCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ConsignmentAgreementId = table.Column<long>(type: "bigint", nullable: false),
                    ProviderProfileId = table.Column<long>(type: "bigint", nullable: false),
                    ProductCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BatchCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TotalKitCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    SettledKitCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TotalSaleAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TotalCommissionAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ProviderPayoutAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PeriodStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ScheduledSettlementDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SettledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SettledByUserId = table.Column<long>(type: "bigint", nullable: true),
                    DisputeReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifyHost = table.Column<string>(type: "text", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    CreateHost = table.Column<string>(type: "text", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sell_through_settlements", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sales_attributions_BatchCode",
                schema: "cargodry",
                table: "sales_attributions",
                column: "BatchCode");

            migrationBuilder.CreateIndex(
                name: "IX_sales_attributions_CommercialModel",
                schema: "cargodry",
                table: "sales_attributions",
                column: "CommercialModel");

            migrationBuilder.CreateIndex(
                name: "IX_sales_attributions_ConsignmentAgreementId",
                schema: "cargodry",
                table: "sales_attributions",
                column: "ConsignmentAgreementId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_attributions_CreatedAtUtc",
                schema: "cargodry",
                table: "sales_attributions",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_sales_attributions_KitId",
                schema: "cargodry",
                table: "sales_attributions",
                column: "KitId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sales_attributions_ProductCode",
                schema: "cargodry",
                table: "sales_attributions",
                column: "ProductCode");

            migrationBuilder.CreateIndex(
                name: "IX_sales_attributions_ProviderProfileId",
                schema: "cargodry",
                table: "sales_attributions",
                column: "ProviderProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_attributions_SalesChannel",
                schema: "cargodry",
                table: "sales_attributions",
                column: "SalesChannel");

            migrationBuilder.CreateIndex(
                name: "IX_sales_attributions_SellThroughSettlementId",
                schema: "cargodry",
                table: "sales_attributions",
                column: "SellThroughSettlementId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_attributions_Status",
                schema: "cargodry",
                table: "sales_attributions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_sell_through_settlements_ConsignmentAgreementId",
                schema: "cargodry",
                table: "sell_through_settlements",
                column: "ConsignmentAgreementId");

            migrationBuilder.CreateIndex(
                name: "IX_sell_through_settlements_ConsignmentAgreementId_ProductCode~",
                schema: "cargodry",
                table: "sell_through_settlements",
                columns: new[] { "ConsignmentAgreementId", "ProductCode", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_sell_through_settlements_PeriodEndUtc",
                schema: "cargodry",
                table: "sell_through_settlements",
                column: "PeriodEndUtc");

            migrationBuilder.CreateIndex(
                name: "IX_sell_through_settlements_PeriodStartUtc",
                schema: "cargodry",
                table: "sell_through_settlements",
                column: "PeriodStartUtc");

            migrationBuilder.CreateIndex(
                name: "IX_sell_through_settlements_ProductCode",
                schema: "cargodry",
                table: "sell_through_settlements",
                column: "ProductCode");

            migrationBuilder.CreateIndex(
                name: "IX_sell_through_settlements_ProviderProfileId",
                schema: "cargodry",
                table: "sell_through_settlements",
                column: "ProviderProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_sell_through_settlements_SettlementCode",
                schema: "cargodry",
                table: "sell_through_settlements",
                column: "SettlementCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sell_through_settlements_Status",
                schema: "cargodry",
                table: "sell_through_settlements",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sales_attributions",
                schema: "cargodry");

            migrationBuilder.DropTable(
                name: "sell_through_settlements",
                schema: "cargodry");

            migrationBuilder.DropColumn(
                name: "ConsignmentAgreementId",
                schema: "cargodry",
                table: "kits");
        }
    }
}
