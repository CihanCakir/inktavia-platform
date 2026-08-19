using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.Identity.Repository.Migrations
{
    /// <inheritdoc />
    public partial class MoveIdentityToOwnSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.RenameTable(
                name: "VerificationDocuments",
                newName: "VerificationDocuments",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "Users",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "UserRoles",
                newName: "UserRoles",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "UserProfiles",
                newName: "UserProfiles",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "UserPasswordHistories",
                newName: "UserPasswordHistories",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "UserMessagePermissions",
                newName: "UserMessagePermissions",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "UserLoginTokens",
                newName: "UserLoginTokens",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "UserExternalLogins",
                newName: "UserExternalLogins",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "UserEmailConfirms",
                newName: "UserEmailConfirms",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "UserDevices",
                newName: "UserDevices",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "UserDeviceBlocks",
                newName: "UserDeviceBlocks",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "UserAgreements",
                newName: "UserAgreements",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "user_validations",
                newName: "user_validations",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "RoleTypeEntity",
                newName: "RoleTypeEntity",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "RiskSignals",
                newName: "RiskSignals",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "provider_service_categories",
                newName: "provider_service_categories",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "provider_password_recovery_requests",
                newName: "provider_password_recovery_requests",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "provider_otp_login_requests",
                newName: "provider_otp_login_requests",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "provider_onboarding",
                newName: "provider_onboarding",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "provider_email_verifications",
                newName: "provider_email_verifications",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "participant_password_recovery_requests",
                newName: "participant_password_recovery_requests",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "participant_otp_login_requests",
                newName: "participant_otp_login_requests",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "AspNetUserTokens",
                newName: "AspNetUserTokens",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "AspNetUserLogins",
                newName: "AspNetUserLogins",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "AspNetUserClaims",
                newName: "AspNetUserClaims",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "AspNetRoles",
                newName: "AspNetRoles",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "AspNetRoleClaims",
                newName: "AspNetRoleClaims",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "Agreements",
                newName: "Agreements",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "admin_otp_login_requests",
                newName: "admin_otp_login_requests",
                newSchema: "identity");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "VerificationDocuments",
                schema: "identity",
                newName: "VerificationDocuments");

            migrationBuilder.RenameTable(
                name: "Users",
                schema: "identity",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "UserRoles",
                schema: "identity",
                newName: "UserRoles");

            migrationBuilder.RenameTable(
                name: "UserProfiles",
                schema: "identity",
                newName: "UserProfiles");

            migrationBuilder.RenameTable(
                name: "UserPasswordHistories",
                schema: "identity",
                newName: "UserPasswordHistories");

            migrationBuilder.RenameTable(
                name: "UserMessagePermissions",
                schema: "identity",
                newName: "UserMessagePermissions");

            migrationBuilder.RenameTable(
                name: "UserLoginTokens",
                schema: "identity",
                newName: "UserLoginTokens");

            migrationBuilder.RenameTable(
                name: "UserExternalLogins",
                schema: "identity",
                newName: "UserExternalLogins");

            migrationBuilder.RenameTable(
                name: "UserEmailConfirms",
                schema: "identity",
                newName: "UserEmailConfirms");

            migrationBuilder.RenameTable(
                name: "UserDevices",
                schema: "identity",
                newName: "UserDevices");

            migrationBuilder.RenameTable(
                name: "UserDeviceBlocks",
                schema: "identity",
                newName: "UserDeviceBlocks");

            migrationBuilder.RenameTable(
                name: "UserAgreements",
                schema: "identity",
                newName: "UserAgreements");

            migrationBuilder.RenameTable(
                name: "user_validations",
                schema: "identity",
                newName: "user_validations");

            migrationBuilder.RenameTable(
                name: "RoleTypeEntity",
                schema: "identity",
                newName: "RoleTypeEntity");

            migrationBuilder.RenameTable(
                name: "RiskSignals",
                schema: "identity",
                newName: "RiskSignals");

            migrationBuilder.RenameTable(
                name: "provider_service_categories",
                schema: "identity",
                newName: "provider_service_categories");

            migrationBuilder.RenameTable(
                name: "provider_password_recovery_requests",
                schema: "identity",
                newName: "provider_password_recovery_requests");

            migrationBuilder.RenameTable(
                name: "provider_otp_login_requests",
                schema: "identity",
                newName: "provider_otp_login_requests");

            migrationBuilder.RenameTable(
                name: "provider_onboarding",
                schema: "identity",
                newName: "provider_onboarding");

            migrationBuilder.RenameTable(
                name: "provider_email_verifications",
                schema: "identity",
                newName: "provider_email_verifications");

            migrationBuilder.RenameTable(
                name: "participant_password_recovery_requests",
                schema: "identity",
                newName: "participant_password_recovery_requests");

            migrationBuilder.RenameTable(
                name: "participant_otp_login_requests",
                schema: "identity",
                newName: "participant_otp_login_requests");

            migrationBuilder.RenameTable(
                name: "AspNetUserTokens",
                schema: "identity",
                newName: "AspNetUserTokens");

            migrationBuilder.RenameTable(
                name: "AspNetUserLogins",
                schema: "identity",
                newName: "AspNetUserLogins");

            migrationBuilder.RenameTable(
                name: "AspNetUserClaims",
                schema: "identity",
                newName: "AspNetUserClaims");

            migrationBuilder.RenameTable(
                name: "AspNetRoles",
                schema: "identity",
                newName: "AspNetRoles");

            migrationBuilder.RenameTable(
                name: "AspNetRoleClaims",
                schema: "identity",
                newName: "AspNetRoleClaims");

            migrationBuilder.RenameTable(
                name: "Agreements",
                schema: "identity",
                newName: "Agreements");

            migrationBuilder.RenameTable(
                name: "admin_otp_login_requests",
                schema: "identity",
                newName: "admin_otp_login_requests");
        }
    }
}
