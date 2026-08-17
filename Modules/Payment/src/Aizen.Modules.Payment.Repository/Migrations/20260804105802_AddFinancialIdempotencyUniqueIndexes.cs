using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.Payment.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialIdempotencyUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_invoice_headers_PaymentTransactionId",
                schema: "payment",
                table: "invoice_headers");

            migrationBuilder.CreateIndex(
                name: "UX_transaction_refund_records_FullRefund_Active",
                schema: "payment",
                table: "transaction_refund_records",
                column: "PaymentTransactionId",
                unique: true,
                filter: "\"RefundType\" = 1 AND \"Status\" <> 4");

            migrationBuilder.CreateIndex(
                name: "UX_payout_records_PaymentTransactionId_Active",
                schema: "payment",
                table: "payout_records",
                column: "PaymentTransactionId",
                unique: true,
                filter: "\"PaymentTransactionId\" IS NOT NULL AND \"Status\" <> 4");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_headers_PaymentTransactionId",
                schema: "payment",
                table: "invoice_headers",
                column: "PaymentTransactionId",
                unique: true,
                filter: "\"PaymentTransactionId\" IS NOT NULL AND \"InvoiceType\" IN (2, 3)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_transaction_refund_records_FullRefund_Active",
                schema: "payment",
                table: "transaction_refund_records");

            migrationBuilder.DropIndex(
                name: "UX_payout_records_PaymentTransactionId_Active",
                schema: "payment",
                table: "payout_records");

            migrationBuilder.DropIndex(
                name: "IX_invoice_headers_PaymentTransactionId",
                schema: "payment",
                table: "invoice_headers");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_headers_PaymentTransactionId",
                schema: "payment",
                table: "invoice_headers",
                column: "PaymentTransactionId",
                filter: "\"PaymentTransactionId\" IS NOT NULL");
        }
    }
}
