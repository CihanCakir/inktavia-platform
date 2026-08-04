using System.Data;
using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Abstraction.Message;
using Aizen.Modules.Messaging.Domain.Entities.Conversation;
using Aizen.Modules.Messaging.Domain.Mapping;
using Aizen.Modules.Messaging.Repository.Persistence;
using Aizen.Modules.Messaging.Repository.Services;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SrEvent = Aizen.Modules.ServiceRequest.Abstraction.Message.ServiceRequestMessageSentMessage;

namespace Aizen.Modules.Messaging.Consumers.ServiceRequest;

/// <summary>
/// PHASE 2 — transitional one-directional live-sync: mirrors each ServiceRequest chat message (the enriched
/// <see cref="SrEvent"/>) into the canonical Messaging store, then republishes <see cref="MessagingMessageSentMessage"/>
/// so the EXISTING admin realtime edge fires (admin sees provider↔owner chat live). Idempotent — dedupes on the SHARED
/// mapper key so it converges with the one-time backfill and survives redelivery. Reads only pre-existing SR rows
/// (owner/accepted-offer) + writes Messaging; never mutates ServiceRequest. Auto-discovered by the messagebus scan
/// (non-generic consumer in the host assembly); no DI registration needed.
/// </summary>
public sealed class ServiceRequestMessageSyncConsumer : AizenBaseMessageConsumer<SrEvent>
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<ServiceRequestMessageSyncConsumer> _logger;

    public ServiceRequestMessageSyncConsumer(IServiceProvider sp) : base(sp)
    {
        _sp = sp;
        _logger = sp.GetRequiredService<ILogger<ServiceRequestMessageSyncConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(SrEvent message, CancellationToken ct) => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(SrEvent m, CancellationToken ct)
    {
        // Nothing to mirror for an empty-content event with no attachment/location (a stray/thin/legacy publish) —
        // skip so no phantom empty message appears in the admin thread.
        if (string.IsNullOrWhiteSpace(m.Content) && m.AttachmentFileId is null && m.LocationLat is null)
        {
            _logger.LogDebug("[SR→Messaging live-sync] empty event skipped SR {SrId}", m.ServiceRequestId);
            return;
        }

        var db = _sp.GetRequiredService<MessagingDbContext>();
        var names = _sp.GetRequiredService<MessagingUserNameResolver>();
        var publisher = _sp.GetRequiredService<IAizenMessagePublisher>();

        // Owner + title + accepted-provider come from ALREADY-committed SR rows (the new SR message row is not read).
        var (ownerUserId, title) = await ReadServiceRequestAsync(db, m.ServiceRequestId, ct);
        if (ownerUserId is null)
        {
            _logger.LogWarning("[SR→Messaging live-sync] SR {SrId} not found — skipped", m.ServiceRequestId);
            return;
        }
        var providerUserId = await ReadAcceptedProviderUserIdAsync(db, m.ServiceRequestId, ct);

        var idsToName = new List<long> { ownerUserId.Value, m.SenderUserId };
        if (providerUserId is { } pu) idsToName.Add(pu);
        var nameMap = await names.ResolveAsync(idsToName, ct);
        string ownerName = nameMap.TryGetValue(ownerUserId.Value, out var on) ? on : "Owner";
        string providerName = providerUserId is { } pp && nameMap.TryGetValue(pp, out var pn) ? pn : "Provider";

        // Ensure conversation (get-or-create by context) — same semantics as EnsureConversationForContext + backfiller.
        var conv = await db.Conversations
            .Include(c => c.Participants).Include(c => c.Messages)
            .FirstOrDefaultAsync(
                c => c.ContextType == MessagingContextType.ServiceRequest && c.ContextId == m.ServiceRequestId, ct);
        if (conv is null)
        {
            var convTitle = string.IsNullOrWhiteSpace(title) ? $"Service Request #{m.ServiceRequestId}" : title;
            conv = ConversationEntity.Create(MessagingContextType.ServiceRequest, m.ServiceRequestId, convTitle);
            conv.AddParticipant(ConversationParticipantEntity.Create(conv.Id, ownerUserId.Value, ownerName, MessagingParticipantRole.Owner));
            if (providerUserId is { } p2)
                conv.AddParticipant(ConversationParticipantEntity.Create(conv.Id, p2, providerName, MessagingParticipantRole.Provider));
            if (m.SenderType == ServiceRequestMessageSenderType.System)
                conv.AddParticipant(ConversationParticipantEntity.Create(conv.Id, 0, "System", MessagingParticipantRole.System));
            await db.Conversations.AddAsync(conv, ct);
        }

        var sentAt = m.OccurredAt ?? DateTimeOffset.UtcNow;
        var content = m.Content ?? string.Empty;
        var senderName = !string.IsNullOrWhiteSpace(m.SenderName)
            ? m.SenderName!.Trim()
            : (nameMap.TryGetValue(m.SenderUserId, out var sn) ? sn : null);

        var key = ServiceRequestMessageMapping.MessageKey(m.SenderUserId, sentAt, content);
        if (conv.Messages.Any(x => ServiceRequestMessageMapping.MessageKey(x.SenderUserId, x.SentAt, x.Content) == key))
        {
            _logger.LogDebug("[SR→Messaging live-sync] duplicate skipped SR {SrId}", m.ServiceRequestId);
            return; // suppress the MessagingMessageSentMessage publish on the dedupe-skip
        }

        var srType = (int)m.SenderType; // SR 1-4 == Messaging role 1-4
        var srMsgType = m.MessageType.HasValue ? (int)m.MessageType.Value : 1;
        var msg = ServiceRequestMessageMapping.MapMessage(
            conv.Id, m.SenderUserId, srType, srMsgType, content, m.AttachmentFileId, sentAt, senderName);
        conv.AddMessage(msg);
        conv.MarkReadByAdmin();
        db.Conversations.Update(conv);
        await db.SaveChangesAsync(ct);

        // Fire the admin realtime edge. RecipientUserIds EMPTY → Notification's consumer returns early (no new/duplicate
        // notification), while the admin socket mapper broadcasts to the admin group regardless of recipients.
        await publisher.PublishAsync(new MessagingMessageSentMessage
        {
            ConversationId    = conv.Id,
            ConversationTitle = conv.Title,
            SenderUserId      = m.SenderUserId,
            SenderName        = senderName ?? ServiceRequestMessageMapping.RoleName((MessagingParticipantRole)srType),
            ContextType       = MessagingContextType.ServiceRequest,
            ContextId         = m.ServiceRequestId,
            RecipientUserIds  = new List<long>(),
            IsInternalNote    = false,
            SentAt            = sentAt,
        }, ct);

        _logger.LogInformation("[SR→Messaging live-sync] synced SR {SrId} → conv {ConvId}", m.ServiceRequestId, conv.Id);
    }

    public override Task ExecuteRollbackMessage(SrEvent message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("[SR→Messaging live-sync] rollback SR {SrId}: {Err}", message.ServiceRequestId, ex.Message);
        return Task.CompletedTask;
    }

    // ── pre-existing SR rows via raw SQL over the shared inktavia_store DB (READ-ONLY) ──
    private static async Task<(long? OwnerUserId, string Title)> ReadServiceRequestAsync(
        MessagingDbContext db, long srId, CancellationToken ct)
    {
        var conn = db.Database.GetDbConnection();
        var wasClosed = conn.State != ConnectionState.Open;
        if (wasClosed) await conn.OpenAsync(ct);
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT \"OwnerUserId\", \"Title\" FROM servicerequest.service_requests WHERE \"Id\" = @id";
            AddParam(cmd, "id", srId);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            if (!await r.ReadAsync(ct)) return (null, string.Empty);
            return (r.GetInt64(0), r.IsDBNull(1) ? string.Empty : r.GetString(1));
        }
        finally { if (wasClosed) await conn.CloseAsync(); }
    }

    private static async Task<long?> ReadAcceptedProviderUserIdAsync(
        MessagingDbContext db, long srId, CancellationToken ct)
    {
        var conn = db.Database.GetDbConnection();
        var wasClosed = conn.State != ConnectionState.Open;
        if (wasClosed) await conn.OpenAsync(ct);
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "SELECT \"ProviderUserId\" FROM servicerequest.service_request_offers " +
                "WHERE \"ServiceRequestId\" = @id AND \"Status\" = 4 ORDER BY \"AcceptedAt\" DESC NULLS LAST LIMIT 1";
            AddParam(cmd, "id", srId);
            var val = await cmd.ExecuteScalarAsync(ct);
            return val is null or DBNull ? null : Convert.ToInt64(val);
        }
        finally { if (wasClosed) await conn.CloseAsync(); }
    }

    private static void AddParam(System.Data.Common.DbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }
}
