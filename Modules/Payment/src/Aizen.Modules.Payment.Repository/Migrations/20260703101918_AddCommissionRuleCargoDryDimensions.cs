using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.Payment.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddCommissionRuleCargoDryDimensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ContextType",
                schema: "payment",
                table: "commission_rules",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductCode",
                schema: "payment",
                table: "commission_rules",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SalesChannel",
                schema: "payment",
                table: "commission_rules",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_commission_rules_ContextType_SalesChannel_Status",
                schema: "payment",
                table: "commission_rules",
                columns: new[] { "ContextType", "SalesChannel", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_commission_rules_ContextType_SalesChannel_Status",
                schema: "payment",
                table: "commission_rules");

            migrationBuilder.DropColumn(
                name: "ContextType",
                schema: "payment",
                table: "commission_rules");

            migrationBuilder.DropColumn(
                name: "ProductCode",
                schema: "payment",
                table: "commission_rules");

            migrationBuilder.DropColumn(
                name: "SalesChannel",
                schema: "payment",
                table: "commission_rules");
        }
    }
}
