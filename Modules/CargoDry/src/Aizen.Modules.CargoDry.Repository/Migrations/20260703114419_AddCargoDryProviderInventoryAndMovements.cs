using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Aizen.Modules.CargoDry.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddCargoDryProviderInventoryAndMovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inventory_movements",
                schema: "cargodry",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProviderProfileId = table.Column<long>(type: "bigint", nullable: false),
                    ProductCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BatchCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    KitId = table.Column<long>(type: "bigint", nullable: true),
                    MovementType = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    BalanceAfter = table.Column<int>(type: "integer", nullable: true),
                    CommercialModel = table.Column<int>(type: "integer", nullable: true),
                    SalesChannel = table.Column<int>(type: "integer", nullable: true),
                    ReferenceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReferenceId = table.Column<long>(type: "bigint", nullable: true),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("PK_inventory_movements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "provider_inventories",
                schema: "cargodry",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProviderProfileId = table.Column<long>(type: "bigint", nullable: false),
                    ProductCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BatchCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CommercialModel = table.Column<int>(type: "integer", nullable: false),
                    SalesChannel = table.Column<int>(type: "integer", nullable: false),
                    StockLocationType = table.Column<int>(type: "integer", nullable: false),
                    TotalAllocated = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TotalActivated = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TotalRevoked = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TotalReturned = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TotalAdjusted = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastMovementAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_provider_inventories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_movements_BatchCode",
                schema: "cargodry",
                table: "inventory_movements",
                column: "BatchCode");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_movements_CreatedAtUtc",
                schema: "cargodry",
                table: "inventory_movements",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_movements_KitId",
                schema: "cargodry",
                table: "inventory_movements",
                column: "KitId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_movements_MovementType",
                schema: "cargodry",
                table: "inventory_movements",
                column: "MovementType");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_movements_ProductCode",
                schema: "cargodry",
                table: "inventory_movements",
                column: "ProductCode");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_movements_ProviderProfileId",
                schema: "cargodry",
                table: "inventory_movements",
                column: "ProviderProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_movements_ProviderProfileId_ProductCode",
                schema: "cargodry",
                table: "inventory_movements",
                columns: new[] { "ProviderProfileId", "ProductCode" });

            migrationBuilder.CreateIndex(
                name: "IX_provider_inventories_BatchCode",
                schema: "cargodry",
                table: "provider_inventories",
                column: "BatchCode");

            migrationBuilder.CreateIndex(
                name: "IX_provider_inventories_CommercialModel",
                schema: "cargodry",
                table: "provider_inventories",
                column: "CommercialModel");

            migrationBuilder.CreateIndex(
                name: "IX_provider_inventories_ProductCode",
                schema: "cargodry",
                table: "provider_inventories",
                column: "ProductCode");

            migrationBuilder.CreateIndex(
                name: "IX_provider_inventories_ProviderProfileId",
                schema: "cargodry",
                table: "provider_inventories",
                column: "ProviderProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_provider_inventories_ProviderProfileId_ProductCode_BatchCode",
                schema: "cargodry",
                table: "provider_inventories",
                columns: new[] { "ProviderProfileId", "ProductCode", "BatchCode" },
                unique: true,
                filter: "\"BatchCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_provider_inventories_SalesChannel",
                schema: "cargodry",
                table: "provider_inventories",
                column: "SalesChannel");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_movements",
                schema: "cargodry");

            migrationBuilder.DropTable(
                name: "provider_inventories",
                schema: "cargodry");
        }
    }
}
