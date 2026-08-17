using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Aizen.Modules.Identity.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderOnboarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "provider_onboarding",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProfileId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SchemaVersion = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    StepStatusesJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    DraftJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    RevisionStepsJson = table.Column<string>(type: "jsonb", nullable: true),
                    RevisionNote = table.Column<string>(type: "text", nullable: true),
                    LastSavedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SubmittedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_provider_onboarding", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_provider_onboarding_ProfileId",
                table: "provider_onboarding",
                column: "ProfileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_onboarding_UserId",
                table: "provider_onboarding",
                column: "UserId");

            // Back-fill: insert a NotStarted row for every existing Organizer profile that has no onboarding row.
            migrationBuilder.Sql(@"
                INSERT INTO provider_onboarding (""ProfileId"", ""UserId"", ""Status"", ""SchemaVersion"",
                    ""StepStatusesJson"", ""DraftJson"", ""IsDeleted"", ""IsActive"", ""CreateDate"")
                SELECT p.""Id"", p.""UserId"", 0, 1, '{}', '{}', false, true, NOW() AT TIME ZONE 'UTC'
                FROM ""UserProfiles"" p
                WHERE p.""RoleContext"" = 3
                  AND p.""IsDeleted"" = false
                  AND NOT EXISTS (
                    SELECT 1 FROM provider_onboarding o WHERE o.""ProfileId"" = p.""Id""
                  )
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "provider_onboarding");
        }
    }
}
