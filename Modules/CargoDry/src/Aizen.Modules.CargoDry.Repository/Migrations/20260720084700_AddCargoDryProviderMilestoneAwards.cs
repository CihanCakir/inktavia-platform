using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Aizen.Modules.CargoDry.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddCargoDryProviderMilestoneAwards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "provider_milestone_awards",
                schema: "cargodry",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProviderProfileId = table.Column<long>(type: "bigint", nullable: false),
                    MilestoneType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PeriodKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DisplayValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AwardedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NotificationPublished = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_milestone_awards", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_provider_milestone_awards_ProviderProfileId_MilestoneType_P~",
                schema: "cargodry",
                table: "provider_milestone_awards",
                columns: new[] { "ProviderProfileId", "MilestoneType", "PeriodKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "provider_milestone_awards",
                schema: "cargodry");
        }
    }
}
