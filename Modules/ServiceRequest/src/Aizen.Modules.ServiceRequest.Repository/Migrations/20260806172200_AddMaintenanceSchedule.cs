using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Aizen.Modules.ServiceRequest.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenanceSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "maintenance_schedules",
                schema: "servicerequest",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VesselId = table.Column<long>(type: "bigint", nullable: false),
                    ServiceCategoryCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ServiceTypeCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OwnerUserId = table.Column<long>(type: "bigint", nullable: false),
                    RecommendedIntervalMonths = table.Column<int>(type: "integer", nullable: false),
                    LastPerformedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextDueAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReminderLeadDays = table.Column<int>(type: "integer", nullable: false),
                    ReminderSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_maintenance_schedules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_schedules_NextDueAt",
                schema: "servicerequest",
                table: "maintenance_schedules",
                column: "NextDueAt");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_schedules_OwnerUserId",
                schema: "servicerequest",
                table: "maintenance_schedules",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_schedules_VesselId_ServiceCategoryCode_ServiceT~",
                schema: "servicerequest",
                table: "maintenance_schedules",
                columns: new[] { "VesselId", "ServiceCategoryCode", "ServiceTypeCode" },
                unique: true,
                filter: "\"IsActive\" = true AND \"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "maintenance_schedules",
                schema: "servicerequest");
        }
    }
}
