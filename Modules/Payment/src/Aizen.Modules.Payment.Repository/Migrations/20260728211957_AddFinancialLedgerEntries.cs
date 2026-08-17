using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Aizen.Modules.Payment.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialLedgerEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "financial_ledger_entries",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EntryCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    AccountLine = table.Column<int>(type: "integer", nullable: false),
                    Nature = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    IsReversal = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CurrencyCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    SourceType = table.Column<int>(type: "integer", nullable: false),
                    SourceRef = table.Column<long>(type: "bigint", nullable: false),
                    TransactionId = table.Column<long>(type: "bigint", nullable: true),
                    ProviderProfileId = table.Column<long>(type: "bigint", nullable: true),
                    CustomerProfileId = table.Column<long>(type: "bigint", nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PostedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_financial_ledger_entries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_financial_ledger_entries_AccountLine_OccurredAtUtc",
                schema: "payment",
                table: "financial_ledger_entries",
                columns: new[] { "AccountLine", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_financial_ledger_entries_CurrencyCode",
                schema: "payment",
                table: "financial_ledger_entries",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_financial_ledger_entries_EntryCode",
                schema: "payment",
                table: "financial_ledger_entries",
                column: "EntryCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_financial_ledger_entries_ProviderProfileId_OccurredAtUtc",
                schema: "payment",
                table: "financial_ledger_entries",
                columns: new[] { "ProviderProfileId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_financial_ledger_entries_SourceType_SourceRef_AccountLine_I~",
                schema: "payment",
                table: "financial_ledger_entries",
                columns: new[] { "SourceType", "SourceRef", "AccountLine", "IsReversal" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "financial_ledger_entries",
                schema: "payment");
        }
    }
}
