using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Aizen.Modules.CargoDry.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddCargoDrySettlementAutomationRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "settlement_automation_runs",
                schema: "cargodry",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RunCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TargetYearMonth = table.Column<int>(type: "integer", nullable: false),
                    Mode = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AutoCompletePayout = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AutoPreparePayment = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AutoPrepareInvoice = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    TriggeredByUserId = table.Column<long>(type: "bigint", nullable: false),
                    TriggeredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true),
                    TotalSettlementsFound = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TotalSettlementsEligible = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TotalSettlementsProcessed = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TotalSettlementsSkipped = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TotalSettlementsErrored = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ErrorSummary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_settlement_automation_runs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "settlement_automation_run_items",
                schema: "cargodry",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RunId = table.Column<long>(type: "bigint", nullable: false),
                    SettlementId = table.Column<long>(type: "bigint", nullable: false),
                    SettlementCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProviderProfileId = table.Column<long>(type: "bigint", nullable: false),
                    ProductCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PeriodYearMonth = table.Column<int>(type: "integer", nullable: false),
                    StatusBefore = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AttributionsResolved = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    AttributionsSkipped = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    AttributionsErrored = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_settlement_automation_run_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_settlement_automation_run_items_settlement_automation_runs_~",
                        column: x => x.RunId,
                        principalSchema: "cargodry",
                        principalTable: "settlement_automation_runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_settlement_automation_run_items_ProviderProfileId",
                schema: "cargodry",
                table: "settlement_automation_run_items",
                column: "ProviderProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_settlement_automation_run_items_RunId",
                schema: "cargodry",
                table: "settlement_automation_run_items",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_settlement_automation_run_items_SettlementId",
                schema: "cargodry",
                table: "settlement_automation_run_items",
                column: "SettlementId");

            migrationBuilder.CreateIndex(
                name: "IX_settlement_automation_run_items_Success",
                schema: "cargodry",
                table: "settlement_automation_run_items",
                column: "Success");

            migrationBuilder.CreateIndex(
                name: "IX_settlement_automation_runs_Mode",
                schema: "cargodry",
                table: "settlement_automation_runs",
                column: "Mode");

            migrationBuilder.CreateIndex(
                name: "IX_settlement_automation_runs_RunCode",
                schema: "cargodry",
                table: "settlement_automation_runs",
                column: "RunCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_settlement_automation_runs_Status",
                schema: "cargodry",
                table: "settlement_automation_runs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_settlement_automation_runs_TargetYearMonth",
                schema: "cargodry",
                table: "settlement_automation_runs",
                column: "TargetYearMonth");

            migrationBuilder.CreateIndex(
                name: "IX_settlement_automation_runs_TriggeredAtUtc",
                schema: "cargodry",
                table: "settlement_automation_runs",
                column: "TriggeredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_settlement_automation_runs_TriggeredByUserId",
                schema: "cargodry",
                table: "settlement_automation_runs",
                column: "TriggeredByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "settlement_automation_run_items",
                schema: "cargodry");

            migrationBuilder.DropTable(
                name: "settlement_automation_runs",
                schema: "cargodry");
        }
    }
}
