using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Aizen.Modules.Identity.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddParticipantOtpLoginRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "participant_otp_login_requests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LoginRequestId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    KeycloakSubjectId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    ParticipantProfileId = table.Column<long>(type: "bigint", nullable: true),
                    Channel = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    TargetHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MaskedTarget = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    OtpHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OtpSalt = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OtpExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    MaxAttempts = table.Column<int>(type: "integer", nullable: false),
                    ConsumedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_participant_otp_login_requests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_participant_otp_login_requests_LoginRequestId",
                table: "participant_otp_login_requests",
                column: "LoginRequestId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "participant_otp_login_requests");
        }
    }
}
