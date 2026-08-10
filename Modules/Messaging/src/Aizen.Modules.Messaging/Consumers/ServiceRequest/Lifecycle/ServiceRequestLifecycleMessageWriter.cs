using System.Data;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Abstraction.Message;
using Aizen.Modules.Messaging.Domain.Entities.Conversation;
using Aizen.Modules.Messaging.Domain.Mapping;
using Aizen.Modules.Messaging.Repository.Persistence;
using Aizen.Modules.Messaging.Repository.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Messaging.Consumers.ServiceRequest.Lifecycle;

/// <summary>
/// BE_WC1 — shared writer for the Messaging-generated System/lifecycle messages (the 5 lifecycle consumers). Given an
/// SR id + a lifecycle CODE, it ensures the ServiceRequest conversation exists (get-or-create with owner/provider
/// participants resolved from the SR schema) and writes ONE <see cref="ConversationMessageEntity"/> with
/// <c>SourceKey = sys:{srId}:{code}</c>. Idempotency is the WC0 partial unique index on (ConversationId, SourceKey):
/// a redelivered event OR the parallel SR→Messaging sync-consumer row (which now keys on the SAME sys: key) collapses
/// to one — the writer swallows the unique-violation as a benign no-op. On a genuine insert it republishes
/// <see cref="MessagingMessageSentMessage"/> with EMPTY recipients so the admin realtime edge fires ONCE while no
/// notification is sent (System/lifecycle/offer are silent). Reads the SR schema read-only; never mutates it.
/// </summary>
public sealed class ServiceRequestLifecycleMessageWriter
{
    private readonly MessagingDbContext _db;
    private readonly MessagingUserNameResolver _names;
    private readonly IAizenMessagePublisher _publisher;
    private readonly ILogger<ServiceRequestLifecycleMessageWriter> _logger;

    public ServiceRequestLifecycleMessageWriter(
        MessagingDbContext db, MessagingUserNameResolver names, IAizenMessagePublisher publisher,
        ILogger<ServiceRequestLifecycleMessageWriter> logger)
    {
        _db = db; _names = names; _publisher = publisher; _logger = logger;
    }

    /// <summary>
    /// Write the lifecycle message for <paramref name="serviceRequestId"/> keyed by <paramref name="code"/>.
    /// <paramref name="senderUserId"/> = 0 → a System-role sender ("System"); otherwise a Provider-role sender (the
    /// offer card), whose display name is resolved.
    /// </summary>
    public async Task WriteAsync(
        long serviceRequestId, string code, MessagingParticipantRole senderRole, MessageType type,
        string content, long senderUserId, CancellationToken ct)
    {
        var (ownerUserId, title) = await ReadServiceRequestAsync(serviceRequestId, ct);
        if (ownerUserId is null)
        {
            _logger.LogWarning("[WC1 lifecycle] SR {SrId} not found — {Code} skipped", serviceRequestId, code);
            return;
        }
        var providerUserId = await ReadAcceptedProviderUserIdAsync(serviceRequestId, ct);

        // Resolve names for the participants we may create + the sender (offer card only).
        var idsToName = new List<long> { ownerUserId.Value };
        if (providerUserId is { } pu) idsToName.Add(pu);
        if (senderUserId != 0) idsToName.Add(senderUserId);
        var nameMap = await _names.ResolveAsync(idsToName, ct);
        string ownerName = nameMap.TryGetValue(ownerUserId.Value, out var on) ? on : "Owner";
        string providerName = providerUserId is { } pp && nameMap.TryGetValue(pp, out var pn) ? pn : "Provider";
        string senderName = senderRole == MessagingParticipantRole.System
            ? "System"
            : (senderUserId != 0 && nameMap.TryGetValue(senderUserId, out var sn) ? sn : providerName);

        var conv = await _db.Conversations
            .Include(c => c.Participants).Include(c => c.Messages)
            .FirstOrDefaultAsync(
                c => c.ContextType == MessagingContextType.ServiceRequest && c.ContextId == serviceRequestId, ct);
        if (conv is null)
        {
            var convTitle = string.IsNullOrWhiteSpace(title) ? $"Service Request #{serviceRequestId}" : title;
            conv = ConversationEntity.Create(MessagingContextType.ServiceRequest, serviceRequestId, convTitle);
            conv.AddParticipant(ConversationParticipantEntity.Create(conv.Id, ownerUserId.Value, ownerName, MessagingParticipantRole.Owner));
            if (providerUserId is { } p2)
                conv.AddParticipant(ConversationParticipantEntity.Create(conv.Id, p2, providerName, MessagingParticipantRole.Provider));
            if (senderRole == MessagingParticipantRole.System)
                conv.AddParticipant(ConversationParticipantEntity.Create(conv.Id, 0, "System", MessagingParticipantRole.System));
            await _db.Conversations.AddAsync(conv, ct);
        }

        var sourceKey = ServiceRequestMessageMapping.SystemSourceKey(serviceRequestId, code);

        // In-process short-circuit: if this exact key is already loaded on the conversation, skip (avoids a guaranteed
        // unique-violation round-trip on same-replica redelivery). The DB index remains the authoritative guard.
        if (conv.Messages.Any(x => x.SourceKey == sourceKey))
        {
            _logger.LogDebug("[WC1 lifecycle] duplicate {Code} skipped SR {SrId}", code, serviceRequestId);
            return;
        }

        var msg = ConversationMessageEntity.Create(
            conv.Id, senderUserId, senderName, senderRole, content, type, isInternalNote: false,
            sentAt: DateTimeOffset.UtcNow, sourceKey: sourceKey);
        conv.AddMessage(msg);
        conv.MarkReadByAdmin();
        _db.Conversations.Update(conv);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // The parallel sync-consumer row (same sys: key) or a concurrent/redelivered event won the insert. Benign
            // no-op; suppress the republish so admin realtime fires exactly once.
            _logger.LogDebug("[WC1 lifecycle] unique-violation {Code} skipped SR {SrId}", code, serviceRequestId);
            return;
        }

        // Admin realtime only — System/lifecycle/offer are SILENT (empty recipients → Notification returns early).
        await _publisher.PublishAsync(new MessagingMessageSentMessage
        {
            ConversationId    = conv.Id,
            ConversationTitle = conv.Title,
            SenderUserId      = senderUserId,
            SenderName        = senderName,
            ContextType       = MessagingContextType.ServiceRequest,
            ContextId         = serviceRequestId,
            RecipientUserIds  = new List<long>(),
            IsInternalNote    = false,
            SentAt            = msg.SentAt,
        }, ct);

        _logger.LogInformation("[WC1 lifecycle] wrote {Code} SR {SrId} → conv {ConvId}", code, serviceRequestId, conv.Id);
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        for (Exception? e = ex; e is not null; e = e.InnerException)
            if (e.GetType().GetProperty("SqlState")?.GetValue(e) as string == "23505") return true;
        return false;
    }

    // ── pre-existing SR rows via raw SQL over the shared inktavia_store DB (READ-ONLY) — mirrors the sync consumer ──
    private async Task<(long? OwnerUserId, string Title)> ReadServiceRequestAsync(long srId, CancellationToken ct)
    {
        var conn = _db.Database.GetDbConnection();
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

    private async Task<long?> ReadAcceptedProviderUserIdAsync(long srId, CancellationToken ct)
    {
        var conn = _db.Database.GetDbConnection();
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
