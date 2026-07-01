using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.Payment.Repository.Migrations
{
    /// <inheritdoc />
    public partial class CommissionAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Priority",
                schema: "payment",
                table: "commission_rules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "ResolvedAppliedCount",
                schema: "payment",
                table: "commission_rules",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "RuleCode",
                schema: "payment",
                table: "commission_rules",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                schema: "payment",
                table: "commission_rules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_commission_rules_RuleCode",
                schema: "payment",
                table: "commission_rules",
                column: "RuleCode",
                unique: true,
                filter: "\"RuleCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_commission_rules_Status",
                schema: "payment",
                table: "commission_rules",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_commission_rules_RuleCode",
                schema: "payment",
                table: "commission_rules");

            migrationBuilder.DropIndex(
                name: "IX_commission_rules_Status",
                schema: "payment",
                table: "commission_rules");

            migrationBuilder.DropColumn(
                name: "Priority",
                schema: "payment",
                table: "commission_rules");

            migrationBuilder.DropColumn(
                name: "ResolvedAppliedCount",
                schema: "payment",
                table: "commission_rules");

            migrationBuilder.DropColumn(
                name: "RuleCode",
                schema: "payment",
                table: "commission_rules");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "payment",
                table: "commission_rules");
        }
    }
}
