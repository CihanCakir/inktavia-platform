using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.CargoDry.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddCargoDryRuleTraceFieldsToSalesAttributions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RateResolvedAtUtc",
                schema: "cargodry",
                table: "sales_attributions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "RateResolvedByUserId",
                schema: "cargodry",
                table: "sales_attributions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ResolvedRate",
                schema: "cargodry",
                table: "sales_attributions",
                type: "numeric(8,4)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ResolvedRuleId",
                schema: "cargodry",
                table: "sales_attributions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolvedRuleName",
                schema: "cargodry",
                table: "sales_attributions",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolvedRuleSource",
                schema: "cargodry",
                table: "sales_attributions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RuleResolutionNote",
                schema: "cargodry",
                table: "sales_attributions",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RateResolvedAtUtc",
                schema: "cargodry",
                table: "sales_attributions");

            migrationBuilder.DropColumn(
                name: "RateResolvedByUserId",
                schema: "cargodry",
                table: "sales_attributions");

            migrationBuilder.DropColumn(
                name: "ResolvedRate",
                schema: "cargodry",
                table: "sales_attributions");

            migrationBuilder.DropColumn(
                name: "ResolvedRuleId",
                schema: "cargodry",
                table: "sales_attributions");

            migrationBuilder.DropColumn(
                name: "ResolvedRuleName",
                schema: "cargodry",
                table: "sales_attributions");

            migrationBuilder.DropColumn(
                name: "ResolvedRuleSource",
                schema: "cargodry",
                table: "sales_attributions");

            migrationBuilder.DropColumn(
                name: "RuleResolutionNote",
                schema: "cargodry",
                table: "sales_attributions");
        }
    }
}
