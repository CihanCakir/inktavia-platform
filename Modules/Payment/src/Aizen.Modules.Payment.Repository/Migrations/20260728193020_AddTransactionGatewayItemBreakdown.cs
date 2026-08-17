using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.Payment.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionGatewayItemBreakdown : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GatewayItemTransactionId",
                schema: "payment",
                table: "transactions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SubMerchantPayoutAmount",
                schema: "payment",
                table: "transactions",
                type: "numeric(18,4)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GatewayItemTransactionId",
                schema: "payment",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "SubMerchantPayoutAmount",
                schema: "payment",
                table: "transactions");
        }
    }
}
