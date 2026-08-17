using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Aizen.Modules.Payment.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddOfferLineAttributeSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "offer_line_attribute_snapshots",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OfferLineEconomicsSnapshotId = table.Column<long>(type: "bigint", nullable: false),
                    DefinitionCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DataType = table.Column<int>(type: "integer", nullable: false),
                    ValueLookupItemCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ValueLookupItemLabel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ValueNumber = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    ValueText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ValueBool = table.Column<bool>(type: "boolean", nullable: true),
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
                    table.PrimaryKey("PK_offer_line_attribute_snapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_offer_line_attribute_snapshots_offer_line_economics_snapsho~",
                        column: x => x.OfferLineEconomicsSnapshotId,
                        principalSchema: "payment",
                        principalTable: "offer_line_economics_snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_offer_line_attribute_snapshots_OfferLineEconomicsSnapshotId",
                schema: "payment",
                table: "offer_line_attribute_snapshots",
                column: "OfferLineEconomicsSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_offer_line_attribute_snapshots_OfferLineEconomicsSnapshotId~",
                schema: "payment",
                table: "offer_line_attribute_snapshots",
                columns: new[] { "OfferLineEconomicsSnapshotId", "DefinitionCode" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "offer_line_attribute_snapshots",
                schema: "payment");
        }
    }
}
