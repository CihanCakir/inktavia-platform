using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.Payment.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddPayoutRecordSourceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "PaymentTransactionId",
                schema: "payment",
                table: "payout_records",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "payment",
                table: "payout_records",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SourceId",
                schema: "payment",
                table: "payout_records",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceType",
                schema: "payment",
                table: "payout_records",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_payout_records_SourceType_SourceId",
                schema: "payment",
                table: "payout_records",
                columns: new[] { "SourceType", "SourceId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_payout_records_SourceType_SourceId",
                schema: "payment",
                table: "payout_records");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "payment",
                table: "payout_records");

            migrationBuilder.DropColumn(
                name: "SourceId",
                schema: "payment",
                table: "payout_records");

            migrationBuilder.DropColumn(
                name: "SourceType",
                schema: "payment",
                table: "payout_records");

            migrationBuilder.AlterColumn<long>(
                name: "PaymentTransactionId",
                schema: "payment",
                table: "payout_records",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }
    }
}
