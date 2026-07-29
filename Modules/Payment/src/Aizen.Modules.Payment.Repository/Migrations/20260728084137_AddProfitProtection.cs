using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Aizen.Modules.Payment.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddProfitProtection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "profit_protection_evaluation_logs",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PolicyId = table.Column<long>(type: "bigint", nullable: true),
                    CurrencyCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DecisionState = table.Column<int>(type: "integer", nullable: false),
                    AdjustmentReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EvaluatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ServiceAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CustomerTotalAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ProviderNetAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    RequestedPlatformFundedDiscount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    RequestedCommissionBenefitCost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    AppliedPlatformFundedDiscount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    AppliedCommissionBenefit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    MaximumSafePlatformFundedDiscount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CustomerSideContributionExpected = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ProviderSideContributionExpected = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TotalTransactionContributionExpected = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    RequiredCustomerSideContribution = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    RequiredProviderSideContribution = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    RequiredTransactionContribution = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
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
                    table.PrimaryKey("PK_profit_protection_evaluation_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "profit_protection_policies",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MinCustomerSideContributionAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    MinCustomerSideContributionRate = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    MinProviderSideContributionAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    MinProviderSideContributionRate = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    MinTransactionContributionAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    MinTransactionContributionRate = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    PaymentProcessingExpenseRate = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    PaymentProcessingFixed = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    RefundRiskReserveRate = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    OtherVariableExpenseRate = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    OtherVariableExpenseFixed = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CustomerSideVariableCostShareRate = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    AdjustmentOrder = table.Column<int>(type: "integer", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PolicyCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PolicyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
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
                    table.PrimaryKey("PK_profit_protection_policies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_profit_protection_evaluation_logs_CurrencyCode_DecisionStat~",
                schema: "payment",
                table: "profit_protection_evaluation_logs",
                columns: new[] { "CurrencyCode", "DecisionState", "EvaluatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_profit_protection_evaluation_logs_PolicyId",
                schema: "payment",
                table: "profit_protection_evaluation_logs",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_profit_protection_policies_CurrencyCode_Status_EffectiveFro~",
                schema: "payment",
                table: "profit_protection_policies",
                columns: new[] { "CurrencyCode", "Status", "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_profit_protection_policies_PolicyCode",
                schema: "payment",
                table: "profit_protection_policies",
                column: "PolicyCode",
                unique: true,
                filter: "\"PolicyCode\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "profit_protection_evaluation_logs",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "profit_protection_policies",
                schema: "payment");
        }
    }
}
