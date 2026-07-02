using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.Payment.Repository.Migrations
{
    /// <inheritdoc />
    public partial class PlanFeatureItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AnnualPriceTRY",
                schema: "payment",
                table: "provider_plans",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BadgeLabel",
                schema: "payment",
                table: "provider_plans",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeatureItems",
                schema: "payment",
                table: "provider_plans",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<int>(
                name: "TrialDays",
                schema: "payment",
                table: "provider_plans",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AnnualPriceTRY",
                schema: "payment",
                table: "participant_plans",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BadgeLabel",
                schema: "payment",
                table: "participant_plans",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeatureItems",
                schema: "payment",
                table: "participant_plans",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<int>(
                name: "TrialDays",
                schema: "payment",
                table: "participant_plans",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnnualPriceTRY",
                schema: "payment",
                table: "provider_plans");

            migrationBuilder.DropColumn(
                name: "BadgeLabel",
                schema: "payment",
                table: "provider_plans");

            migrationBuilder.DropColumn(
                name: "FeatureItems",
                schema: "payment",
                table: "provider_plans");

            migrationBuilder.DropColumn(
                name: "TrialDays",
                schema: "payment",
                table: "provider_plans");

            migrationBuilder.DropColumn(
                name: "AnnualPriceTRY",
                schema: "payment",
                table: "participant_plans");

            migrationBuilder.DropColumn(
                name: "BadgeLabel",
                schema: "payment",
                table: "participant_plans");

            migrationBuilder.DropColumn(
                name: "FeatureItems",
                schema: "payment",
                table: "participant_plans");

            migrationBuilder.DropColumn(
                name: "TrialDays",
                schema: "payment",
                table: "participant_plans");
        }
    }
}
