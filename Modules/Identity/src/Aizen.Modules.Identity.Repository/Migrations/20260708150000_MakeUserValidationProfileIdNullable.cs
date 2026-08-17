using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.Identity.Repository.Migrations
{
    /// <inheritdoc />
    public partial class MakeUserValidationProfileIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_validations_UserProfiles_UserProfileId",
                table: "user_validations");

            migrationBuilder.AlterColumn<long>(
                name: "UserProfileId",
                table: "user_validations",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddForeignKey(
                name: "FK_user_validations_UserProfiles_UserProfileId",
                table: "user_validations",
                column: "UserProfileId",
                principalTable: "UserProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_validations_UserProfiles_UserProfileId",
                table: "user_validations");

            migrationBuilder.AlterColumn<long>(
                name: "UserProfileId",
                table: "user_validations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldNullable: true,
                oldType: "bigint");

            migrationBuilder.AddForeignKey(
                name: "FK_user_validations_UserProfiles_UserProfileId",
                table: "user_validations",
                column: "UserProfileId",
                principalTable: "UserProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
