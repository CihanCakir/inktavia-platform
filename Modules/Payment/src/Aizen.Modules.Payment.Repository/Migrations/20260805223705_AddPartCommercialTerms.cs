using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Aizen.Modules.Payment.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddPartCommercialTerms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "part_commercial_terms",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProductCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProviderProfileId = table.Column<long>(type: "bigint", nullable: true),
                    CategoryCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CurrencyCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    SupplierListPrice = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ProviderDealerMargin = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    MaxCustomerDiscount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    SupplierFundedAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ProviderFundedAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    PlatformFundedAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    MinimumProviderReceivable = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    MaximumDiscountableAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TermCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TermName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
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
                    table.PrimaryKey("PK_part_commercial_terms", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_part_commercial_terms_CurrencyCode_ProductCode_ProviderProf~",
                schema: "payment",
                table: "part_commercial_terms",
                columns: new[] { "CurrencyCode", "ProductCode", "ProviderProfileId", "Brand", "CategoryCode", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_part_commercial_terms_Status_EffectiveFrom_EffectiveTo",
                schema: "payment",
                table: "part_commercial_terms",
                columns: new[] { "Status", "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_part_commercial_terms_TermCode",
                schema: "payment",
                table: "part_commercial_terms",
                column: "TermCode",
                unique: true,
                filter: "\"TermCode\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "part_commercial_terms",
                schema: "payment");
        }
    }
}
