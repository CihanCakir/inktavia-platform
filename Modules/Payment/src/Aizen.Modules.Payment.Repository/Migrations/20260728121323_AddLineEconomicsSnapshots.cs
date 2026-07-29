using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Aizen.Modules.Payment.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddLineEconomicsSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "OriginalServiceGrossAmountSnapshot",
                schema: "payment",
                table: "payment_economics_snapshots",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ServiceVatTotalSnapshot",
                schema: "payment",
                table: "payment_economics_snapshots",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCustomerDiscountSnapshot",
                schema: "payment",
                table: "payment_economics_snapshots",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalPlatformFundedDiscountSnapshot",
                schema: "payment",
                table: "payment_economics_snapshots",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalProviderFundedDiscountSnapshot",
                schema: "payment",
                table: "payment_economics_snapshots",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "commission_allocation_snapshots",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EconomicsSnapshotId = table.Column<long>(type: "bigint", nullable: false),
                    LineRef = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CommissionRuleId = table.Column<long>(type: "bigint", nullable: true),
                    CommissionRuleCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CommissionBaseAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ResolvedRate = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Commissionable = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_commission_allocation_snapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_commission_allocation_snapshots_payment_economics_snapshots~",
                        column: x => x.EconomicsSnapshotId,
                        principalSchema: "payment",
                        principalTable: "payment_economics_snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "discount_allocation_snapshots",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EconomicsSnapshotId = table.Column<long>(type: "bigint", nullable: false),
                    LineRef = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FundingSource = table.Column<int>(type: "integer", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    RuleCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_discount_allocation_snapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_discount_allocation_snapshots_payment_economics_snapshots_E~",
                        column: x => x.EconomicsSnapshotId,
                        principalSchema: "payment",
                        principalTable: "payment_economics_snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "offer_line_economics_snapshots",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EconomicsSnapshotId = table.Column<long>(type: "bigint", nullable: false),
                    LineRef = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ItemType = table.Column<int>(type: "integer", nullable: false),
                    PricingMethod = table.Column<int>(type: "integer", nullable: false),
                    LineGrossBeforeDiscount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CustomerDiscountAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ProviderFundedDiscountAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    PlatformFundedDiscountAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CommissionEligibility = table.Column<int>(type: "integer", nullable: false),
                    CommissionBaseAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CommissionRate = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ProviderNetAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    LineVatAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    LineTotalAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_offer_line_economics_snapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_offer_line_economics_snapshots_payment_economics_snapshots_~",
                        column: x => x.EconomicsSnapshotId,
                        principalSchema: "payment",
                        principalTable: "payment_economics_snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_commission_allocation_snapshots_EconomicsSnapshotId",
                schema: "payment",
                table: "commission_allocation_snapshots",
                column: "EconomicsSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_commission_allocation_snapshots_EconomicsSnapshotId_LineRef",
                schema: "payment",
                table: "commission_allocation_snapshots",
                columns: new[] { "EconomicsSnapshotId", "LineRef" });

            migrationBuilder.CreateIndex(
                name: "IX_discount_allocation_snapshots_EconomicsSnapshotId",
                schema: "payment",
                table: "discount_allocation_snapshots",
                column: "EconomicsSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_discount_allocation_snapshots_EconomicsSnapshotId_LineRef",
                schema: "payment",
                table: "discount_allocation_snapshots",
                columns: new[] { "EconomicsSnapshotId", "LineRef" });

            migrationBuilder.CreateIndex(
                name: "IX_offer_line_economics_snapshots_EconomicsSnapshotId",
                schema: "payment",
                table: "offer_line_economics_snapshots",
                column: "EconomicsSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_offer_line_economics_snapshots_EconomicsSnapshotId_LineRef",
                schema: "payment",
                table: "offer_line_economics_snapshots",
                columns: new[] { "EconomicsSnapshotId", "LineRef" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "commission_allocation_snapshots",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "discount_allocation_snapshots",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "offer_line_economics_snapshots",
                schema: "payment");

            migrationBuilder.DropColumn(
                name: "OriginalServiceGrossAmountSnapshot",
                schema: "payment",
                table: "payment_economics_snapshots");

            migrationBuilder.DropColumn(
                name: "ServiceVatTotalSnapshot",
                schema: "payment",
                table: "payment_economics_snapshots");

            migrationBuilder.DropColumn(
                name: "TotalCustomerDiscountSnapshot",
                schema: "payment",
                table: "payment_economics_snapshots");

            migrationBuilder.DropColumn(
                name: "TotalPlatformFundedDiscountSnapshot",
                schema: "payment",
                table: "payment_economics_snapshots");

            migrationBuilder.DropColumn(
                name: "TotalProviderFundedDiscountSnapshot",
                schema: "payment",
                table: "payment_economics_snapshots");
        }
    }
}
