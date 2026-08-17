using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.Payment.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddCommissionRuleLineDimsAndResolutionIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CommissionEligibility",
                schema: "payment",
                table: "commission_rules",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LineType",
                schema: "payment",
                table: "commission_rules",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_commission_rules_IsActive_EffectiveFrom_EffectiveTo",
                schema: "payment",
                table: "commission_rules",
                columns: new[] { "IsActive", "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_commission_rules_RuleType_CategoryCode",
                schema: "payment",
                table: "commission_rules",
                columns: new[] { "RuleType", "CategoryCode" });

            migrationBuilder.CreateIndex(
                name: "IX_commission_rules_RuleType_ProviderPlanId",
                schema: "payment",
                table: "commission_rules",
                columns: new[] { "RuleType", "ProviderPlanId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_commission_rules_IsActive_EffectiveFrom_EffectiveTo",
                schema: "payment",
                table: "commission_rules");

            migrationBuilder.DropIndex(
                name: "IX_commission_rules_RuleType_CategoryCode",
                schema: "payment",
                table: "commission_rules");

            migrationBuilder.DropIndex(
                name: "IX_commission_rules_RuleType_ProviderPlanId",
                schema: "payment",
                table: "commission_rules");

            migrationBuilder.DropColumn(
                name: "CommissionEligibility",
                schema: "payment",
                table: "commission_rules");

            migrationBuilder.DropColumn(
                name: "LineType",
                schema: "payment",
                table: "commission_rules");
        }
    }
}
