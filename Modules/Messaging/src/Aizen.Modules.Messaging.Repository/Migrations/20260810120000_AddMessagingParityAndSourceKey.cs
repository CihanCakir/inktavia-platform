using Aizen.Modules.Messaging.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.Messaging.Repository.Migrations
{
    /// <summary>
    /// BE_WC0 — Messaging-store parity + durable idempotency index (Phase-4 safety net). Additive, reversible, zero
    /// behaviour change:
    ///   • add the location parity columns (LocationLat/Lng/Label) — nullable, unused until WC1/WC2;
    ///   • add the SourceKey natural-key column (nullable during the transition);
    ///   • backfill SourceKey on historical synced rows (correlated to the SR message id via the shared natural key)
    ///     and collapse any pre-existing redelivery duplicates — guarded so it no-ops on a DB without the SR schema;
    ///   • add the PARTIAL UNIQUE index on (ConversationId, SourceKey) WHERE SourceKey IS NOT NULL — the durable,
    ///     multi-replica idempotency guard that replaces the sync consumer's in-process SemaphoreSlim.
    /// The columns are nullable and the index is partial, so native Messaging sends (null SourceKey) are unaffected.
    /// </summary>
    [DbContext(typeof(MessagingDbContext))]
    [Migration("20260810120000_AddMessagingParityAndSourceKey")]
    public partial class AddMessagingParityAndSourceKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "LocationLat",
                schema: "messaging",
                table: "conversation_messages",
                type: "numeric(10,7)",
                precision: 10,
                scale: 7,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LocationLng",
                schema: "messaging",
                table: "conversation_messages",
                type: "numeric(10,7)",
                precision: 10,
                scale: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationLabel",
                schema: "messaging",
                table: "conversation_messages",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceKey",
                schema: "messaging",
                table: "conversation_messages",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            // ── Backfill SourceKey on historical synced rows + collapse pre-existing duplicates. Runs BEFORE the
            //    unique index so the index can build cleanly. Guarded by to_regclass so it is a no-op on a scratch /
            //    isolated Messaging DB that has no ServiceRequest schema (the migration still applies + rolls back).
            //    Idempotent: the UPDATE only touches null keys; a re-run finds nothing to change or collapse.
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF to_regclass('servicerequest.service_request_messages') IS NOT NULL THEN

        -- (a) Correlate each historical Messaging message back to its ServiceRequest message row via the SHARED
        --     natural key (SenderUserId, whole-second timestamp, trimmed Content) — the same key the sync consumer
        --     dedupes on today — and stamp the stable SourceKey 'sr:{serviceRequestId}:{srMessageId}'.
        --     (comma-join form: in Postgres UPDATE..FROM the target 'cm' can only be referenced in SET/WHERE).
        UPDATE messaging.conversation_messages cm
        SET ""SourceKey"" = 'sr:' || conv.""ContextId"" || ':' || srm.""Id""
        FROM messaging.conversations conv,
             servicerequest.service_request_messages srm
        WHERE cm.""ConversationId"" = conv.""Id""
          AND conv.""ContextType"" = 1              -- MessagingContextType.ServiceRequest
          AND cm.""SourceKey"" IS NULL
          AND srm.""ServiceRequestId"" = conv.""ContextId""
          AND srm.""SenderUserId"" = cm.""SenderUserId""
          AND floor(extract(epoch FROM srm.""CreateDate"")) = floor(extract(epoch FROM cm.""SentAt""))
          AND btrim(srm.""Content"") = btrim(cm.""Content"");

        -- (b) Collapse any pre-existing duplicate rows that now share the same (ConversationId, SourceKey) — the
        --     redelivery double-row race the in-process semaphore could not always stop — keeping the lowest Id.
        --     Child attachments cascade (FK ON DELETE CASCADE). Required so the partial unique index below builds.
        DELETE FROM messaging.conversation_messages a
        USING messaging.conversation_messages b
        WHERE a.""SourceKey"" IS NOT NULL
          AND a.""ConversationId"" = b.""ConversationId""
          AND a.""SourceKey"" = b.""SourceKey""
          AND a.""Id"" > b.""Id"";

    END IF;
END $$;");

            migrationBuilder.CreateIndex(
                name: "UX_conversation_messages_ConversationId_SourceKey",
                schema: "messaging",
                table: "conversation_messages",
                columns: new[] { "ConversationId", "SourceKey" },
                unique: true,
                filter: "\"SourceKey\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_conversation_messages_ConversationId_SourceKey",
                schema: "messaging",
                table: "conversation_messages");

            migrationBuilder.DropColumn(
                name: "SourceKey",
                schema: "messaging",
                table: "conversation_messages");

            migrationBuilder.DropColumn(
                name: "LocationLabel",
                schema: "messaging",
                table: "conversation_messages");

            migrationBuilder.DropColumn(
                name: "LocationLng",
                schema: "messaging",
                table: "conversation_messages");

            migrationBuilder.DropColumn(
                name: "LocationLat",
                schema: "messaging",
                table: "conversation_messages");
        }
    }
}
