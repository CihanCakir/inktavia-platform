using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.CargoDry.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddCargoDrySettlementPayoutCompletionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PayoutCompletedAtUtc",
                schema: "cargodry",
                table: "sell_through_settlements",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PayoutCompletedByUserId",
                schema: "cargodry",
                table: "sell_through_settlements",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayoutCompletionReference",
                schema: "cargodry",
                table: "sell_through_settlements",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayoutFailureReason",
                schema: "cargodry",
                table: "sell_through_settlements",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayoutLifecycleNote",
                schema: "cargodry",
                table: "sell_through_settlements",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PayoutCompletedAtUtc",
                schema: "cargodry",
                table: "sell_through_settlements");

            migrationBuilder.DropColumn(
                name: "PayoutCompletedByUserId",
                schema: "cargodry",
                table: "sell_through_settlements");

            migrationBuilder.DropColumn(
                name: "PayoutCompletionReference",
                schema: "cargodry",
                table: "sell_through_settlements");

            migrationBuilder.DropColumn(
                name: "PayoutFailureReason",
                schema: "cargodry",
                table: "sell_through_settlements");

            migrationBuilder.DropColumn(
                name: "PayoutLifecycleNote",
                schema: "cargodry",
                table: "sell_through_settlements");
        }
    }
}
