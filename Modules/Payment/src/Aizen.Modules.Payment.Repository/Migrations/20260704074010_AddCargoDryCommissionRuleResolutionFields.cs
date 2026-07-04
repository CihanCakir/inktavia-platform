using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.Payment.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddCargoDryCommissionRuleResolutionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CommercialModel",
                schema: "payment",
                table: "commission_rules",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                schema: "payment",
                table: "commission_rules",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RuleName",
                schema: "payment",
                table: "commission_rules",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommercialModel",
                schema: "payment",
                table: "commission_rules");

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                schema: "payment",
                table: "commission_rules");

            migrationBuilder.DropColumn(
                name: "RuleName",
                schema: "payment",
                table: "commission_rules");
        }
    }
}
