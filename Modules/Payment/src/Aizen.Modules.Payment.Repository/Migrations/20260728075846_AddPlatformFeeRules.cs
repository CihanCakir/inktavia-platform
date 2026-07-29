using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Aizen.Modules.Payment.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformFeeRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "platform_fee_rules",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Model = table.Column<int>(type: "integer", nullable: false),
                    Rate = table.Column<decimal>(type: "numeric(9,4)", nullable: true),
                    FixedAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    MinAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    MaxAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    CurrencyCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CategoryCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CustomerType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RuleCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    RuleName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    VatRate = table.Column<decimal>(type: "numeric(9,4)", nullable: true),
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
                    table.PrimaryKey("PK_platform_fee_rules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_platform_fee_rules_CurrencyCode_CategoryCode_CustomerType",
                schema: "payment",
                table: "platform_fee_rules",
                columns: new[] { "CurrencyCode", "CategoryCode", "CustomerType" });

            migrationBuilder.CreateIndex(
                name: "IX_platform_fee_rules_RuleCode",
                schema: "payment",
                table: "platform_fee_rules",
                column: "RuleCode",
                unique: true,
                filter: "\"RuleCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_platform_fee_rules_Status_EffectiveFrom_EffectiveTo",
                schema: "payment",
                table: "platform_fee_rules",
                columns: new[] { "Status", "EffectiveFrom", "EffectiveTo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "platform_fee_rules",
                schema: "payment");
        }
    }
}
