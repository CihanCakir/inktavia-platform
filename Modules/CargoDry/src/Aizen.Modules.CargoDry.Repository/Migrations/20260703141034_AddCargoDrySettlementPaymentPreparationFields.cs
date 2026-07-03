using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.CargoDry.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddCargoDrySettlementPaymentPreparationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentPreparationNote",
                schema: "cargodry",
                table: "sell_through_settlements",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentPreparedAtUtc",
                schema: "cargodry",
                table: "sell_through_settlements",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PaymentPreparedByUserId",
                schema: "cargodry",
                table: "sell_through_settlements",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PayoutRecordId",
                schema: "cargodry",
                table: "sell_through_settlements",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentPreparationNote",
                schema: "cargodry",
                table: "sell_through_settlements");

            migrationBuilder.DropColumn(
                name: "PaymentPreparedAtUtc",
                schema: "cargodry",
                table: "sell_through_settlements");

            migrationBuilder.DropColumn(
                name: "PaymentPreparedByUserId",
                schema: "cargodry",
                table: "sell_through_settlements");

            migrationBuilder.DropColumn(
                name: "PayoutRecordId",
                schema: "cargodry",
                table: "sell_through_settlements");
        }
    }
}
