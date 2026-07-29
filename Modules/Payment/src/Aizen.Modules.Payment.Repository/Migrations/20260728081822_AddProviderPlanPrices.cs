using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Aizen.Modules.Payment.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderPlanPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "provider_plan_prices",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProviderPlanId = table.Column<long>(type: "bigint", nullable: false),
                    PriceType = table.Column<int>(type: "integer", nullable: false),
                    BillingPeriod = table.Column<int>(type: "integer", nullable: false),
                    PriceAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PriceCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
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
                    table.PrimaryKey("PK_provider_plan_prices", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_provider_plan_prices_EffectiveFrom_EffectiveTo",
                schema: "payment",
                table: "provider_plan_prices",
                columns: new[] { "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_provider_plan_prices_PriceCode",
                schema: "payment",
                table: "provider_plan_prices",
                column: "PriceCode",
                unique: true,
                filter: "\"PriceCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_provider_plan_prices_ProviderPlanId_CurrencyCode_BillingPer~",
                schema: "payment",
                table: "provider_plan_prices",
                columns: new[] { "ProviderPlanId", "CurrencyCode", "BillingPeriod", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "provider_plan_prices",
                schema: "payment");
        }
    }
}
