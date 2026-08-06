using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Aizen.Modules.Payment.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddLineProfitProtectionS9 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DefaultAllowedPlatformFundedDiscountRate",
                schema: "payment",
                table: "profit_protection_policies",
                type: "numeric(9,4)",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultAllowedProviderFundedDiscountRate",
                schema: "payment",
                table: "profit_protection_policies",
                type: "numeric(9,4)",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultLineMinProviderReceivableAmount",
                schema: "payment",
                table: "profit_protection_policies",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultLineMinProviderReceivableRate",
                schema: "payment",
                table: "profit_protection_policies",
                type: "numeric(9,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LineCommissionFloorRate",
                schema: "payment",
                table: "profit_protection_policies",
                type: "numeric(9,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MinLinePlatformContributionRate",
                schema: "payment",
                table: "profit_protection_policies",
                type: "numeric(9,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "StrategicLossExceptionEnabled",
                schema: "payment",
                table: "profit_protection_policies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "StrategicLossExceptionMaxLineDeficit",
                schema: "payment",
                table: "profit_protection_policies",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LineMinProviderReceivableApplied",
                schema: "payment",
                table: "offer_line_economics_snapshots",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LinePlatformContribution",
                schema: "payment",
                table: "offer_line_economics_snapshots",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "LineProfitProtectionPassed",
                schema: "payment",
                table: "offer_line_economics_snapshots",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "line_profit_protection_evaluation_logs",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServiceRequestId = table.Column<long>(type: "bigint", nullable: false),
                    OfferId = table.Column<long>(type: "bigint", nullable: false),
                    PolicyId = table.Column<long>(type: "bigint", nullable: true),
                    CurrencyCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DecisionState = table.Column<int>(type: "integer", nullable: false),
                    EvaluatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LineCount = table.Column<int>(type: "integer", nullable: false),
                    FailedLineCount = table.Column<int>(type: "integer", nullable: false),
                    PrimaryBreachCode = table.Column<int>(type: "integer", nullable: true),
                    FailedLineRefs = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_line_profit_protection_evaluation_logs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_line_profit_protection_evaluation_logs_CurrencyCode_Decisio~",
                schema: "payment",
                table: "line_profit_protection_evaluation_logs",
                columns: new[] { "CurrencyCode", "DecisionState", "EvaluatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_line_profit_protection_evaluation_logs_ServiceRequestId_Off~",
                schema: "payment",
                table: "line_profit_protection_evaluation_logs",
                columns: new[] { "ServiceRequestId", "OfferId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "line_profit_protection_evaluation_logs",
                schema: "payment");

            migrationBuilder.DropColumn(
                name: "DefaultAllowedPlatformFundedDiscountRate",
                schema: "payment",
                table: "profit_protection_policies");

            migrationBuilder.DropColumn(
                name: "DefaultAllowedProviderFundedDiscountRate",
                schema: "payment",
                table: "profit_protection_policies");

            migrationBuilder.DropColumn(
                name: "DefaultLineMinProviderReceivableAmount",
                schema: "payment",
                table: "profit_protection_policies");

            migrationBuilder.DropColumn(
                name: "DefaultLineMinProviderReceivableRate",
                schema: "payment",
                table: "profit_protection_policies");

            migrationBuilder.DropColumn(
                name: "LineCommissionFloorRate",
                schema: "payment",
                table: "profit_protection_policies");

            migrationBuilder.DropColumn(
                name: "MinLinePlatformContributionRate",
                schema: "payment",
                table: "profit_protection_policies");

            migrationBuilder.DropColumn(
                name: "StrategicLossExceptionEnabled",
                schema: "payment",
                table: "profit_protection_policies");

            migrationBuilder.DropColumn(
                name: "StrategicLossExceptionMaxLineDeficit",
                schema: "payment",
                table: "profit_protection_policies");

            migrationBuilder.DropColumn(
                name: "LineMinProviderReceivableApplied",
                schema: "payment",
                table: "offer_line_economics_snapshots");

            migrationBuilder.DropColumn(
                name: "LinePlatformContribution",
                schema: "payment",
                table: "offer_line_economics_snapshots");

            migrationBuilder.DropColumn(
                name: "LineProfitProtectionPassed",
                schema: "payment",
                table: "offer_line_economics_snapshots");
        }
    }
}
