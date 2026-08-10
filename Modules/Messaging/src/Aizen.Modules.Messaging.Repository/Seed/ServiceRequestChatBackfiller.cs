using System.Data;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Domain.Entities.Conversation;
using Aizen.Modules.Messaging.Domain.Mapping;
using Aizen.Modules.Messaging.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Messaging.Repository.Seed;

/// <summary>
/// SPIKE P1 — one-time, idempotent, reversible backfill of the ServiceRequest module's provider↔owner chat
/// (<c>servicerequest.service_request_messages</c>) into the canonical Messaging store as
/// <c>ContextType=ServiceRequest</c> conversations. READS the ServiceRequest schema (same <c>inktavia_store</c> DB,
/// via the Messaging connection) and WRITES only the Messaging schema — never mutates ServiceRequest data. It does
/// not repoint any send/read/realtime path (Phases 2–5). Uses the same get-or-create ensure semantics as the
/// <c>EnsureConversationForContext</c> command (P0). Idempotent: messages are de-duplicated on the natural key
/// (SenderUserId, SentAt, Content) with the ORIGINAL source timestamp preserved, so a re-run converges.
/// </summary>
public sealed class ServiceRequestChatBackfiller
{
    private readonly MessagingDbContext _db;
    private readonly ILogger<ServiceRequestChatBackfiller> _logger;

    public ServiceRequestChatBackfiller(MessagingDbContext db, ILogger<ServiceRequestChatBackfiller> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<BackfillResult> BackfillAsync(CancellationToken ct = default)
    {
        var chats = await ReadServiceRequestChatsAsync(ct);
        var result = new BackfillResult { ServiceRequestsWithChat = chats.Count };

        foreach (var chat in chats)
        {
            try
            {
                await BackfillOneAsync(chat, result, ct);
            }
            catch (Exception ex)
            {
                result.Errors++;
                _logger.LogError(ex, "[SR→Messaging backfill] failed for ServiceRequest {SrId}", chat.ServiceRequestId);
            }
        }

        _logger.LogInformation(
            "[SR→Messaging backfill] done: {Srs} SR chats · conversations created {Created}, reused {Reused} · " +
            "messages inserted {Inserted}, skipped(existing) {Skipped} · participants {Participants} · errors {Errors}",
            result.ServiceRequestsWithChat, result.ConversationsCreated, result.ConversationsReused,
            result.MessagesInserted, result.MessagesSkipped, result.ParticipantsCreated, result.Errors);

        return result;
    }

    private async Task BackfillOneAsync(SrChat chat, BackfillResult result, CancellationToken ct)
    {
        var conv = await _db.Conversations
            .Include(c => c.Participants)
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(
                c => c.ContextType == MessagingContextType.ServiceRequest && c.ContextId == chat.ServiceRequestId, ct);

        var created = conv is null;
        if (created)
        {
            var title = string.IsNullOrWhiteSpace(chat.Title) ? $"Service Request #{chat.ServiceRequestId}" : chat.Title;
            conv = ConversationEntity.Create(MessagingContextType.ServiceRequest, chat.ServiceRequestId, title);

            conv.AddParticipant(ConversationParticipantEntity.Create(
                conv.Id, chat.OwnerUserId, "Owner", MessagingParticipantRole.Owner));
            result.ParticipantsCreated++;

            if (chat.ProviderUserId is { } providerId)
            {
                conv.AddParticipant(ConversationParticipantEntity.Create(
                    conv.Id, providerId, "Provider", MessagingParticipantRole.Provider));
                result.ParticipantsCreated++;
            }

            if (chat.Messages.Any(m => m.SenderType == 4)) // System
            {
                conv.AddParticipant(ConversationParticipantEntity.Create(
                    conv.Id, 0, "System", MessagingParticipantRole.System));
                result.ParticipantsCreated++;
            }

            await _db.Conversations.AddAsync(conv, ct);
            result.ConversationsCreated++;
        }
        else
        {
            result.ConversationsReused++;
        }

        // Idempotency via the SHARED mapper's key (identical to the live-sync consumer, so the two converge).
        var seen = new HashSet<string>(
            conv!.Messages.Select(m => ServiceRequestMessageMapping.MessageKey(m.SenderUserId, m.SentAt, m.Content)));

        var insertedHere = 0;
        foreach (var sm in chat.Messages) // already ordered by CreateDate, Id
        {
            var sentAt = new DateTimeOffset(DateTime.SpecifyKind(sm.CreateDate, DateTimeKind.Utc));
            var key = ServiceRequestMessageMapping.MessageKey(sm.SenderUserId, sentAt, sm.Content);
            if (!seen.Add(key)) { result.MessagesSkipped++; continue; }

            // senderName null → role placeholder; the name-fix pass + live-sync populate real names.
            // BE_WC0 — stamp the durable SourceKey (sr:{srId}:{srMessageId}) so imported rows carry the same key the
            // live-sync consumer writes and the partial unique index applies cleanly.
            var msg = ServiceRequestMessageMapping.MapMessage(
                conv.Id, sm.SenderUserId, sm.SenderType, sm.MessageType, sm.Content, sm.AttachmentFileId, sentAt,
                senderName: null, locationLat: null, locationLng: null, locationLabel: null,
                sourceKey: ServiceRequestMessageMapping.SourceKey(chat.ServiceRequestId, sm.Id));

            conv.AddMessage(msg);
            result.MessagesInserted++;
            insertedHere++;
        }

        // Historical import — do not surface these as fresh unread for the admin.
        if (insertedHere > 0)
            conv.MarkReadByAdmin();

        if (!created)
            _db.Conversations.Update(conv);

        await _db.SaveChangesAsync(ct);
    }

    // ── Read side: raw SQL over the shared inktavia_store DB (servicerequest schema). READ-ONLY. ──────────────────
    private async Task<List<SrChat>> ReadServiceRequestChatsAsync(CancellationToken ct)
    {
        var conn = _db.Database.GetDbConnection();
        var wasClosed = conn.State != ConnectionState.Open;
        if (wasClosed) await conn.OpenAsync(ct);
        try
        {
            var ids = new List<long>();
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText =
                    "SELECT DISTINCT \"ServiceRequestId\" FROM servicerequest.service_request_messages ORDER BY 1";
                await using var r = await cmd.ExecuteReaderAsync(ct);
                while (await r.ReadAsync(ct)) ids.Add(r.GetInt64(0));
            }

            var chats = new List<SrChat>();
            foreach (var srId in ids)
            {
                var (ownerUserId, title) = await ReadOwnerAsync(conn, srId, ct);
                if (ownerUserId is null)
                {
                    _logger.LogWarning("[SR→Messaging backfill] SR {SrId} has chat but no service_requests row — skipped", srId);
                    continue;
                }
                var providerUserId = await ReadAcceptedProviderAsync(conn, srId, ct);
                var messages = await ReadMessagesAsync(conn, srId, ct);
                chats.Add(new SrChat(srId, ownerUserId.Value, title, providerUserId, messages));
            }
            return chats;
        }
        finally
        {
            if (wasClosed) await conn.CloseAsync();
        }
    }

    private static async Task<(long? OwnerUserId, string Title)> ReadOwnerAsync(
        System.Data.Common.DbConnection conn, long srId, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT \"OwnerUserId\", \"Title\" FROM servicerequest.service_requests WHERE \"Id\" = @id";
        AddParam(cmd, "id", srId);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return (null, string.Empty);
        var owner = r.GetInt64(0);
        var title = r.IsDBNull(1) ? string.Empty : r.GetString(1);
        return (owner, title);
    }

    private static async Task<long?> ReadAcceptedProviderAsync(
        System.Data.Common.DbConnection conn, long srId, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT \"ProviderUserId\" FROM servicerequest.service_request_offers " +
            "WHERE \"ServiceRequestId\" = @id AND \"Status\" = 4 ORDER BY \"AcceptedAt\" DESC NULLS LAST LIMIT 1";
        AddParam(cmd, "id", srId);
        var val = await cmd.ExecuteScalarAsync(ct);
        return val is null or DBNull ? null : Convert.ToInt64(val);
    }

    private static async Task<List<SrMsg>> ReadMessagesAsync(
        System.Data.Common.DbConnection conn, long srId, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT \"SenderUserId\", \"SenderType\", \"MessageType\", \"Content\", \"AttachmentFileId\", \"CreateDate\", \"Id\" " +
            "FROM servicerequest.service_request_messages WHERE \"ServiceRequestId\" = @id ORDER BY \"CreateDate\", \"Id\"";
        AddParam(cmd, "id", srId);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        var list = new List<SrMsg>();
        while (await r.ReadAsync(ct))
        {
            list.Add(new SrMsg(
                SenderUserId: r.GetInt64(0),
                SenderType: r.GetInt32(1),
                MessageType: r.GetInt32(2),
                Content: r.IsDBNull(3) ? string.Empty : r.GetString(3),
                AttachmentFileId: r.IsDBNull(4) ? null : r.GetGuid(4),
                CreateDate: r.IsDBNull(5) ? DateTime.UtcNow : r.GetDateTime(5),
                Id: r.GetInt64(6)));
        }
        return list;
    }

    private static void AddParam(System.Data.Common.DbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }

    private sealed record SrChat(long ServiceRequestId, long OwnerUserId, string Title, long? ProviderUserId, List<SrMsg> Messages);
    private sealed record SrMsg(long SenderUserId, int SenderType, int MessageType, string Content, Guid? AttachmentFileId, DateTime CreateDate, long Id);

    public sealed class BackfillResult
    {
        public int ServiceRequestsWithChat { get; set; }
        public int ConversationsCreated    { get; set; }
        public int ConversationsReused     { get; set; }
        public int MessagesInserted        { get; set; }
        public int MessagesSkipped         { get; set; }
        public int ParticipantsCreated     { get; set; }
        public int Errors                  { get; set; }
    }
}
