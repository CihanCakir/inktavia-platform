using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Aizen.Modules.Payment.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddTravelPricingSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "travel_pricing_snapshots",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EconomicsSnapshotId = table.Column<long>(type: "bigint", nullable: false),
                    LineRef = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Method = table.Column<int>(type: "integer", nullable: false),
                    OriginCityCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    OriginCityLabel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DestinationCityCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DestinationCityLabel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DistanceKm = table.Column<decimal>(type: "numeric(12,3)", nullable: true),
                    PerKmRate = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    UnitCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ResolvedTravelAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
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
                    table.PrimaryKey("PK_travel_pricing_snapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_travel_pricing_snapshots_payment_economics_snapshots_Econom~",
                        column: x => x.EconomicsSnapshotId,
                        principalSchema: "payment",
                        principalTable: "payment_economics_snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_travel_pricing_snapshots_EconomicsSnapshotId",
                schema: "payment",
                table: "travel_pricing_snapshots",
                column: "EconomicsSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_travel_pricing_snapshots_EconomicsSnapshotId_LineRef",
                schema: "payment",
                table: "travel_pricing_snapshots",
                columns: new[] { "EconomicsSnapshotId", "LineRef" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "travel_pricing_snapshots",
                schema: "payment");
        }
    }
}
