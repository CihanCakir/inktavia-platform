using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Aizen.Modules.CargoDry.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddCargoDryKitLifecycleEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "kit_lifecycle_events",
                schema: "cargodry",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KitId = table.Column<long>(type: "bigint", nullable: false),
                    KitCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BatchCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ProductCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EventType = table.Column<int>(type: "integer", nullable: false),
                    PreviousStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    NewStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ActorUserId = table.Column<long>(type: "bigint", nullable: true),
                    ActorType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReferenceId = table.Column<long>(type: "bigint", nullable: true),
                    ReferenceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_kit_lifecycle_events", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_kit_lifecycle_events_ActorUserId",
                schema: "cargodry",
                table: "kit_lifecycle_events",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_kit_lifecycle_events_BatchCode",
                schema: "cargodry",
                table: "kit_lifecycle_events",
                column: "BatchCode");

            migrationBuilder.CreateIndex(
                name: "IX_kit_lifecycle_events_EventType",
                schema: "cargodry",
                table: "kit_lifecycle_events",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_kit_lifecycle_events_KitCode",
                schema: "cargodry",
                table: "kit_lifecycle_events",
                column: "KitCode");

            migrationBuilder.CreateIndex(
                name: "IX_kit_lifecycle_events_KitId",
                schema: "cargodry",
                table: "kit_lifecycle_events",
                column: "KitId");

            migrationBuilder.CreateIndex(
                name: "IX_kit_lifecycle_events_OccurredAtUtc",
                schema: "cargodry",
                table: "kit_lifecycle_events",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_kit_lifecycle_events_ProductCode",
                schema: "cargodry",
                table: "kit_lifecycle_events",
                column: "ProductCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "kit_lifecycle_events",
                schema: "cargodry");
        }
    }
}
