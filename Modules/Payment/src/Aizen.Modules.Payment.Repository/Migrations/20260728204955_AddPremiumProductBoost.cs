using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Aizen.Modules.Payment.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddPremiumProductBoost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "premium_entitlements",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PremiumPurchaseId = table.Column<long>(type: "bigint", nullable: false),
                    ProviderProfileId = table.Column<long>(type: "bigint", nullable: false),
                    ProductCodeSnapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ContextRef = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevocationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_premium_entitlements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "premium_product_prices",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PremiumProductId = table.Column<long>(type: "bigint", nullable: false),
                    PriceAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PriceCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_premium_product_prices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "premium_products",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EntitlementType = table.Column<int>(type: "integer", nullable: false),
                    DurationDays = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_premium_products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "premium_purchases",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProviderProfileId = table.Column<long>(type: "bigint", nullable: false),
                    PremiumProductId = table.Column<long>(type: "bigint", nullable: false),
                    ProductCodeSnapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PremiumProductPriceIdSnapshot = table.Column<long>(type: "bigint", nullable: false),
                    UnitPriceSnapshot = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CurrencyCodeSnapshot = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DurationDaysSnapshot = table.Column<int>(type: "integer", nullable: false),
                    ContextRef = table.Column<long>(type: "bigint", nullable: false),
                    PaymentTransactionId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PurchaseCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
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
                    table.PrimaryKey("PK_premium_purchases", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_premium_entitlements_ContextRef_Status",
                schema: "payment",
                table: "premium_entitlements",
                columns: new[] { "ContextRef", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_premium_entitlements_PremiumPurchaseId",
                schema: "payment",
                table: "premium_entitlements",
                column: "PremiumPurchaseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_premium_entitlements_Status_ExpiresAt",
                schema: "payment",
                table: "premium_entitlements",
                columns: new[] { "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_premium_product_prices_EffectiveFrom_EffectiveTo",
                schema: "payment",
                table: "premium_product_prices",
                columns: new[] { "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_premium_product_prices_PremiumProductId_CurrencyCode_Status",
                schema: "payment",
                table: "premium_product_prices",
                columns: new[] { "PremiumProductId", "CurrencyCode", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_premium_product_prices_PriceCode",
                schema: "payment",
                table: "premium_product_prices",
                column: "PriceCode",
                unique: true,
                filter: "\"PriceCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_premium_products_Code",
                schema: "payment",
                table: "premium_products",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_premium_purchases_PaymentTransactionId",
                schema: "payment",
                table: "premium_purchases",
                column: "PaymentTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_premium_purchases_ProviderProfileId_ContextRef_Status",
                schema: "payment",
                table: "premium_purchases",
                columns: new[] { "ProviderProfileId", "ContextRef", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_premium_purchases_PurchaseCode",
                schema: "payment",
                table: "premium_purchases",
                column: "PurchaseCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "premium_entitlements",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "premium_product_prices",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "premium_products",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "premium_purchases",
                schema: "payment");
        }
    }
}
