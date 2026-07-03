using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.CargoDry.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddCargoDryCommercialFoundationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ConsignmentPrice",
                schema: "cargodry",
                table: "products",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ProviderCommissionRate",
                schema: "cargodry",
                table: "products",
                type: "numeric(6,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WholesalePrice",
                schema: "cargodry",
                table: "products",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CommercialModel",
                schema: "cargodry",
                table: "kits",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "InvoiceId",
                schema: "cargodry",
                table: "kits",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PaymentTransactionId",
                schema: "cargodry",
                table: "kits",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ProviderProfileId",
                schema: "cargodry",
                table: "kits",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SalesChannel",
                schema: "cargodry",
                table: "kits",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StockLocationType",
                schema: "cargodry",
                table: "kits",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<long>(
                name: "AssignedProviderProfileId",
                schema: "cargodry",
                table: "batches",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CommercialModel",
                schema: "cargodry",
                table: "batches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ConsignmentAgreementId",
                schema: "cargodry",
                table: "batches",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_kits_ProviderProfileId",
                schema: "cargodry",
                table: "kits",
                column: "ProviderProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_kits_SalesChannel_Status",
                schema: "cargodry",
                table: "kits",
                columns: new[] { "SalesChannel", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_batches_AssignedProviderProfileId",
                schema: "cargodry",
                table: "batches",
                column: "AssignedProviderProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_kits_ProviderProfileId",
                schema: "cargodry",
                table: "kits");

            migrationBuilder.DropIndex(
                name: "IX_kits_SalesChannel_Status",
                schema: "cargodry",
                table: "kits");

            migrationBuilder.DropIndex(
                name: "IX_batches_AssignedProviderProfileId",
                schema: "cargodry",
                table: "batches");

            migrationBuilder.DropColumn(
                name: "ConsignmentPrice",
                schema: "cargodry",
                table: "products");

            migrationBuilder.DropColumn(
                name: "ProviderCommissionRate",
                schema: "cargodry",
                table: "products");

            migrationBuilder.DropColumn(
                name: "WholesalePrice",
                schema: "cargodry",
                table: "products");

            migrationBuilder.DropColumn(
                name: "CommercialModel",
                schema: "cargodry",
                table: "kits");

            migrationBuilder.DropColumn(
                name: "InvoiceId",
                schema: "cargodry",
                table: "kits");

            migrationBuilder.DropColumn(
                name: "PaymentTransactionId",
                schema: "cargodry",
                table: "kits");

            migrationBuilder.DropColumn(
                name: "ProviderProfileId",
                schema: "cargodry",
                table: "kits");

            migrationBuilder.DropColumn(
                name: "SalesChannel",
                schema: "cargodry",
                table: "kits");

            migrationBuilder.DropColumn(
                name: "StockLocationType",
                schema: "cargodry",
                table: "kits");

            migrationBuilder.DropColumn(
                name: "AssignedProviderProfileId",
                schema: "cargodry",
                table: "batches");

            migrationBuilder.DropColumn(
                name: "CommercialModel",
                schema: "cargodry",
                table: "batches");

            migrationBuilder.DropColumn(
                name: "ConsignmentAgreementId",
                schema: "cargodry",
                table: "batches");
        }
    }
}
