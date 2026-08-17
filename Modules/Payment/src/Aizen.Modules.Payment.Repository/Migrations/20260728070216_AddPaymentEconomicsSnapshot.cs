using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Aizen.Modules.Payment.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentEconomicsSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "EconomicsSnapshotId",
                schema: "payment",
                table: "transactions",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "payment_economics_snapshots",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SnapshotCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ContextType = table.Column<int>(type: "integer", nullable: false),
                    ContextId = table.Column<long>(type: "bigint", nullable: false),
                    CurrencyCodeSnapshot = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    RoundingModeSnapshot = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ServiceAmountSnapshot = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ServiceVatAmountSnapshot = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ServiceGrossAmountSnapshot = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CustomerPayableServiceAmountSnapshot = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CommissionBaseAmountSnapshot = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CommissionRateSnapshot = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    CommissionAmountSnapshot = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ProviderNetAmountSnapshot = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    PlatformFeeBaseAmountSnapshot = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    PlatformFeeRuleIdSnapshot = table.Column<long>(type: "bigint", nullable: true),
                    PlatformFeeRateSnapshot = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    PlatformFeeMinimumSnapshot = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    PlatformFeeMaximumSnapshot = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    PlatformFeeNetAmountSnapshot = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    PlatformFeeVatAmountSnapshot = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    PlatformFeeGrossAmountSnapshot = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CustomerTotalAmountSnapshot = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    PlatformGrossShareSnapshot = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifyHost = table.Column<string>(type: "text", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    CreateHost = table.Column<string>(type: "text", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_economics_snapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_transactions_EconomicsSnapshotId",
                schema: "payment",
                table: "transactions",
                column: "EconomicsSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_payment_economics_snapshots_ContextType_ContextId",
                schema: "payment",
                table: "payment_economics_snapshots",
                columns: new[] { "ContextType", "ContextId" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_economics_snapshots_SnapshotCode",
                schema: "payment",
                table: "payment_economics_snapshots",
                column: "SnapshotCode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_transactions_payment_economics_snapshots_EconomicsSnapshotId",
                schema: "payment",
                table: "transactions",
                column: "EconomicsSnapshotId",
                principalSchema: "payment",
                principalTable: "payment_economics_snapshots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_transactions_payment_economics_snapshots_EconomicsSnapshotId",
                schema: "payment",
                table: "transactions");

            migrationBuilder.DropTable(
                name: "payment_economics_snapshots",
                schema: "payment");

            migrationBuilder.DropIndex(
                name: "IX_transactions_EconomicsSnapshotId",
                schema: "payment",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "EconomicsSnapshotId",
                schema: "payment",
                table: "transactions");
        }
    }
}
