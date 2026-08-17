using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Aizen.Modules.Payment.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundAllocationChargeback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BenefitRestoreApplied",
                schema: "payment",
                table: "transaction_refund_records",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Cause",
                schema: "payment",
                table: "transaction_refund_records",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "RefundAllocationId",
                schema: "payment",
                table: "transaction_refund_records",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReleaseState",
                schema: "payment",
                table: "transaction_refund_records",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "chargeback_records",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PaymentTransactionId = table.Column<long>(type: "bigint", nullable: false),
                    GatewayChargebackReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ChargebackExpenseAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ProviderRecoveredAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    RemainingNegativeBalance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_chargeback_records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "provider_balances",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProviderProfileId = table.Column<long>(type: "bigint", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    NegativeBalanceLimit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_provider_balances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "refund_allocation_policies",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CurrencyCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PolicyCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    PolicyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    NegativeBalanceLimit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
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
                    table.PrimaryKey("PK_refund_allocation_policies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "refund_allocations",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RefundRecordId = table.Column<long>(type: "bigint", nullable: false),
                    EconomicsSnapshotId = table.Column<long>(type: "bigint", nullable: false),
                    Cause = table.Column<int>(type: "integer", nullable: false),
                    ReleaseState = table.Column<int>(type: "integer", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ServiceRefundAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ProviderNetReversalAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CommissionRevenueReversalAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    PlatformFeeNetRefundAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    PlatformFeeVatRefundAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    PlatformFeeGrossRefundAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    GatewayRefundExpenseAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ProviderRecoveryAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    PlatformAdvancedRefundAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    RemainingProviderNegativeBalance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
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
                    table.PrimaryKey("PK_refund_allocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "provider_balance_movements",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProviderBalanceId = table.Column<long>(type: "bigint", nullable: false),
                    MovementType = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    RefundRecordId = table.Column<long>(type: "bigint", nullable: true),
                    ChargebackRecordId = table.Column<long>(type: "bigint", nullable: true),
                    AdminUserId = table.Column<long>(type: "bigint", nullable: true),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_provider_balance_movements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_provider_balance_movements_provider_balances_ProviderBalanc~",
                        column: x => x.ProviderBalanceId,
                        principalSchema: "payment",
                        principalTable: "provider_balances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refund_allocation_policy_rules",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RefundAllocationPolicyId = table.Column<long>(type: "bigint", nullable: false),
                    Cause = table.Column<int>(type: "integer", nullable: false),
                    PlatformFeeRefundMode = table.Column<int>(type: "integer", nullable: false),
                    FixedPlatformFeeAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
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
                    table.PrimaryKey("PK_refund_allocation_policy_rules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_refund_allocation_policy_rules_refund_allocation_policies_R~",
                        column: x => x.RefundAllocationPolicyId,
                        principalSchema: "payment",
                        principalTable: "refund_allocation_policies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_chargeback_records_GatewayChargebackReference",
                schema: "payment",
                table: "chargeback_records",
                column: "GatewayChargebackReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chargeback_records_PaymentTransactionId",
                schema: "payment",
                table: "chargeback_records",
                column: "PaymentTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_provider_balance_movements_ProviderBalanceId",
                schema: "payment",
                table: "provider_balance_movements",
                column: "ProviderBalanceId");

            migrationBuilder.CreateIndex(
                name: "IX_provider_balances_ProviderProfileId_CurrencyCode",
                schema: "payment",
                table: "provider_balances",
                columns: new[] { "ProviderProfileId", "CurrencyCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refund_allocation_policies_CurrencyCode_Status_EffectiveFro~",
                schema: "payment",
                table: "refund_allocation_policies",
                columns: new[] { "CurrencyCode", "Status", "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_refund_allocation_policies_PolicyCode",
                schema: "payment",
                table: "refund_allocation_policies",
                column: "PolicyCode",
                unique: true,
                filter: "\"PolicyCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_refund_allocation_policy_rules_RefundAllocationPolicyId_Cau~",
                schema: "payment",
                table: "refund_allocation_policy_rules",
                columns: new[] { "RefundAllocationPolicyId", "Cause" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refund_allocations_EconomicsSnapshotId",
                schema: "payment",
                table: "refund_allocations",
                column: "EconomicsSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_refund_allocations_RefundRecordId",
                schema: "payment",
                table: "refund_allocations",
                column: "RefundRecordId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chargeback_records",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "provider_balance_movements",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "refund_allocation_policy_rules",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "refund_allocations",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "provider_balances",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "refund_allocation_policies",
                schema: "payment");

            migrationBuilder.DropColumn(
                name: "BenefitRestoreApplied",
                schema: "payment",
                table: "transaction_refund_records");

            migrationBuilder.DropColumn(
                name: "Cause",
                schema: "payment",
                table: "transaction_refund_records");

            migrationBuilder.DropColumn(
                name: "RefundAllocationId",
                schema: "payment",
                table: "transaction_refund_records");

            migrationBuilder.DropColumn(
                name: "ReleaseState",
                schema: "payment",
                table: "transaction_refund_records");
        }
    }
}
