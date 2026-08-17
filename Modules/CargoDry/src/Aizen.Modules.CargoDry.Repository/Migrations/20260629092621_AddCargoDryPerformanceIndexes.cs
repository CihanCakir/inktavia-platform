using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.CargoDry.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddCargoDryPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_kits_ActivatedAt",
                schema: "cargodry",
                table: "kits",
                column: "ActivatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_kits_ProductCode_Status",
                schema: "cargodry",
                table: "kits",
                columns: new[] { "ProductCode", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_batches_IsRevoked",
                schema: "cargodry",
                table: "batches",
                column: "IsRevoked");

            migrationBuilder.CreateIndex(
                name: "IX_batches_ProductCode",
                schema: "cargodry",
                table: "batches",
                column: "ProductCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_kits_ActivatedAt",
                schema: "cargodry",
                table: "kits");

            migrationBuilder.DropIndex(
                name: "IX_kits_ProductCode_Status",
                schema: "cargodry",
                table: "kits");

            migrationBuilder.DropIndex(
                name: "IX_batches_IsRevoked",
                schema: "cargodry",
                table: "batches");

            migrationBuilder.DropIndex(
                name: "IX_batches_ProductCode",
                schema: "cargodry",
                table: "batches");
        }
    }
}
