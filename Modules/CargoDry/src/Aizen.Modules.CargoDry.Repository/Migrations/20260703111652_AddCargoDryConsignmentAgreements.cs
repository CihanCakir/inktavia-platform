using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Aizen.Modules.CargoDry.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddCargoDryConsignmentAgreements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "consignment_agreements",
                schema: "cargodry",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AgreementCode = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    ProviderProfileId = table.Column<long>(type: "bigint", nullable: false),
                    ProductCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ConsignmentRate = table.Column<decimal>(type: "numeric(6,4)", nullable: false),
                    MinimumSettlementAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false, defaultValue: "TRY"),
                    MaxKitCount = table.Column<int>(type: "integer", nullable: false),
                    AllocatedKitCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TermsDocumentRef = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ActivatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SuspendedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TerminatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SuspendReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TerminationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_consignment_agreements", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_consignment_agreements_AgreementCode",
                schema: "cargodry",
                table: "consignment_agreements",
                column: "AgreementCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_consignment_agreements_EndDateUtc",
                schema: "cargodry",
                table: "consignment_agreements",
                column: "EndDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_consignment_agreements_ProductCode",
                schema: "cargodry",
                table: "consignment_agreements",
                column: "ProductCode");

            migrationBuilder.CreateIndex(
                name: "IX_consignment_agreements_ProviderProfileId",
                schema: "cargodry",
                table: "consignment_agreements",
                column: "ProviderProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_consignment_agreements_ProviderProfileId_ProductCode_Status",
                schema: "cargodry",
                table: "consignment_agreements",
                columns: new[] { "ProviderProfileId", "ProductCode", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_consignment_agreements_StartDateUtc",
                schema: "cargodry",
                table: "consignment_agreements",
                column: "StartDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_consignment_agreements_Status",
                schema: "cargodry",
                table: "consignment_agreements",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "consignment_agreements",
                schema: "cargodry");
        }
    }
}
