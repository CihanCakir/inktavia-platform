using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Aizen.Modules.Payment.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerDiscountAndBenefit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customer_benefit_budget_policies",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerPlanId = table.Column<long>(type: "bigint", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    BenefitBudgetRate = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    PerPeriodMax = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    PerCategoryLimit = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    PerTransactionLimit = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    RefundRestorePolicy = table.Column<int>(type: "integer", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PolicyCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
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
                    table.PrimaryKey("PK_customer_benefit_budget_policies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "customer_benefit_budgets",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ParticipantPlanSubscriptionId = table.Column<long>(type: "bigint", nullable: false),
                    CustomerPlanId = table.Column<long>(type: "bigint", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FundedAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ReservedAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ConsumedAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_customer_benefit_budgets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "customer_benefit_reservations",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BudgetId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ContextRef = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ReservedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsumedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReleasedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_customer_benefit_reservations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "customer_discount_rules",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerPlanId = table.Column<long>(type: "bigint", nullable: true),
                    CategoryCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CurrencyCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DiscountType = table.Column<int>(type: "integer", nullable: false),
                    DiscountRate = table.Column<decimal>(type: "numeric(9,4)", nullable: true),
                    FixedDiscountAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    MinimumPurchaseAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    MaximumDiscountAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    FundingMode = table.Column<int>(type: "integer", nullable: false),
                    PlatformFundingRate = table.Column<decimal>(type: "numeric(9,4)", nullable: true),
                    ProviderFundingRate = table.Column<decimal>(type: "numeric(9,4)", nullable: true),
                    RequiresProviderConsent = table.Column<bool>(type: "boolean", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RuleCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    RuleName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
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
                    table.PrimaryKey("PK_customer_discount_rules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_customer_benefit_budget_policies_CustomerPlanId_CurrencyCod~",
                schema: "payment",
                table: "customer_benefit_budget_policies",
                columns: new[] { "CustomerPlanId", "CurrencyCode", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_customer_benefit_budget_policies_PolicyCode",
                schema: "payment",
                table: "customer_benefit_budget_policies",
                column: "PolicyCode",
                unique: true,
                filter: "\"PolicyCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_customer_benefit_budgets_ParticipantPlanSubscriptionId_Stat~",
                schema: "payment",
                table: "customer_benefit_budgets",
                columns: new[] { "ParticipantPlanSubscriptionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_customer_benefit_reservations_BudgetId_ContextRef",
                schema: "payment",
                table: "customer_benefit_reservations",
                columns: new[] { "BudgetId", "ContextRef" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_benefit_reservations_Status",
                schema: "payment",
                table: "customer_benefit_reservations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_customer_discount_rules_CurrencyCode_CustomerPlanId_Categor~",
                schema: "payment",
                table: "customer_discount_rules",
                columns: new[] { "CurrencyCode", "CustomerPlanId", "CategoryCode", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_customer_discount_rules_RuleCode",
                schema: "payment",
                table: "customer_discount_rules",
                column: "RuleCode",
                unique: true,
                filter: "\"RuleCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_customer_discount_rules_Status_EffectiveFrom_EffectiveTo",
                schema: "payment",
                table: "customer_discount_rules",
                columns: new[] { "Status", "EffectiveFrom", "EffectiveTo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_benefit_budget_policies",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "customer_benefit_budgets",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "customer_benefit_reservations",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "customer_discount_rules",
                schema: "payment");
        }
    }
}
