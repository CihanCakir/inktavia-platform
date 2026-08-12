using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.Notification.Repository.Migrations
{
    /// <summary>
    /// BE_NF1b — one-time backfill: re-key existing OWNER-facing notifications from the Identity USER id to the owner's
    /// participant PROFILE id (the id the owner's mobile inbox + device tokens resolve to). Owner rows were filed under
    /// <c>SR.OwnerUserId</c> (a user id) while the inbox resolves the participant profile id, so the owner saw 0 history.
    ///
    /// Scope is the participant population only: rows whose RecipientUserId matches a <c>UserProfiles</c> row with
    /// <c>RoleContext = 1 (Participant)</c>. Provider notifications (filed under ProviderProfileId, an Organizer-context
    /// profile) are NOT touched. Runs exactly once (tracked in __EFMigrationsHistory) — a re-runnable seeder would be
    /// unsafe because a profile id can itself be another participant's user id (dense id space → cascade).
    /// </summary>
    public partial class NF1bBackfillOwnerRecipientKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Set-based UPDATE evaluated against the original table state (no row-by-row cascade). Only owner rows under a
            // participant user id are re-keyed to that participant's profile id; already-aligned rows (Id == UserId) skip.
            migrationBuilder.Sql(@"
                UPDATE notification.notifications AS n
                SET ""RecipientUserId"" = up.""Id""
                FROM public.""UserProfiles"" AS up
                WHERE up.""UserId"" = n.""RecipientUserId""
                  AND up.""RoleContext"" = 1
                  AND up.""Id"" <> n.""RecipientUserId"";
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Irreversible data re-key; no safe automatic down-migration (the original user id is not retained per row).
        }
    }
}
