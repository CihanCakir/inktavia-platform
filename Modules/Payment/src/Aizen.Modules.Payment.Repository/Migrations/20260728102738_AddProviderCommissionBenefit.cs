using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Aizen.Modules.Payment.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderCommissionBenefit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "provider_commission_benefit_entitlements",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EntitlementCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ProviderProfileId = table.Column<long>(type: "bigint", nullable: false),
                    BenefitRuleId = table.Column<long>(type: "bigint", nullable: false),
                    GrantedFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GrantedTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UsageLimit = table.Column<long>(type: "bigint", nullable: true),
                    UsedCount = table.Column<long>(type: "bigint", nullable: false),
                    ReservedCount = table.Column<long>(type: "bigint", nullable: false),
                    MaximumEligibleGMV = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    ConsumedGMV = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ReservedGMV = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
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
                    table.PrimaryKey("PK_provider_commission_benefit_entitlements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "provider_commission_benefit_rules",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RuleCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    RuleName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ProviderProfileId = table.Column<long>(type: "bigint", nullable: true),
                    ProviderPlanId = table.Column<long>(type: "bigint", nullable: true),
                    ApplicableCategoryCodesCsv = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AdjustmentPercentagePoints = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    MinimumCommissionRate = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    MaximumDiscountAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    MaximumEligibleGMV = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    UsageLimit = table.Column<long>(type: "bigint", nullable: true),
                    Stackable = table.Column<bool>(type: "boolean", nullable: false),
                    Exclusive = table.Column<bool>(type: "boolean", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CurrencyCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_provider_commission_benefit_rules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "provider_commission_benefit_usages",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EntitlementId = table.Column<long>(type: "bigint", nullable: false),
                    ContextRef = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    GmvAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    BenefitAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_provider_commission_benefit_usages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_provider_commission_benefit_entitlements_EntitlementCode",
                schema: "payment",
                table: "provider_commission_benefit_entitlements",
                column: "EntitlementCode",
                unique: true,
                filter: "\"EntitlementCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_provider_commission_benefit_entitlements_ProviderProfileId_~",
                schema: "payment",
                table: "provider_commission_benefit_entitlements",
                columns: new[] { "ProviderProfileId", "BenefitRuleId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_provider_commission_benefit_rules_CurrencyCode_Status_Effec~",
                schema: "payment",
                table: "provider_commission_benefit_rules",
                columns: new[] { "CurrencyCode", "Status", "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_provider_commission_benefit_rules_ProviderProfileId_Provide~",
                schema: "payment",
                table: "provider_commission_benefit_rules",
                columns: new[] { "ProviderProfileId", "ProviderPlanId" });

            migrationBuilder.CreateIndex(
                name: "IX_provider_commission_benefit_rules_RuleCode",
                schema: "payment",
                table: "provider_commission_benefit_rules",
                column: "RuleCode",
                unique: true,
                filter: "\"RuleCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_provider_commission_benefit_usages_EntitlementId_ContextRef",
                schema: "payment",
                table: "provider_commission_benefit_usages",
                columns: new[] { "EntitlementId", "ContextRef" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_commission_benefit_usages_Status",
                schema: "payment",
                table: "provider_commission_benefit_usages",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "provider_commission_benefit_entitlements",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "provider_commission_benefit_rules",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "provider_commission_benefit_usages",
                schema: "payment");
        }
    }
}
