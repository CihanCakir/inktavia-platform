using System.Collections.Concurrent;
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

    // Per-service-request serialization gate (see ExecuteCommitMessage). Static: shared across the singleton
    // consumer's concurrent invocations in this process.
    private static readonly ConcurrentDictionary<long, SemaphoreSlim> _srGates = new();

    public override async Task ExecuteCommitMessage(SrEvent m, CancellationToken ct)
    {
        // Nothing to mirror for an empty-content event with no attachment/location (a stray/thin/legacy publish) —
        // skip so no phantom empty message appears in the admin thread.
        if (string.IsNullOrWhiteSpace(m.Content) && m.AttachmentFileId is null && m.LocationLat is null)
        {
            _logger.LogDebug("[SR→Messaging live-sync] empty event skipped SR {SrId}", m.ServiceRequestId);
            return;
        }

        // Serialize concurrent commit deliveries for the SAME service request. The two-phase bus can deliver the
        // commit for one SR message more than once, concurrently (each consumer of the SR event republishes a commit
        // onto the shared exchange), and the in-memory dedup in SyncOneAsync is TOCTOU-racy across concurrent
        // invocations — without this gate both invocations load conv.Messages before either saves, so both insert
        // (observed: two identical rows in the same thread). A per-SR SemaphoreSlim closes the window on the
        // single-replica messaging-api; a DB unique index on the message key is the durable multi-replica fix.
        var gate = _srGates.GetOrAdd(m.ServiceRequestId, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            await SyncOneAsync(m, ct);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task SyncOneAsync(SrEvent m, CancellationToken ct)
    {
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
        // BE_WC0/WC1 — stamp the durable idempotency key. Lifecycle messages (System pills + the offer card) key on
        // sys:{srId}:{CODE} so this sync row and the WC1 Messaging-generated row COLLAPSE to one via the partial unique
        // index during the parallel run; regular chat (Text/Image/Location) keys on sr:{srId}:{srMessageId}. The
        // per-SR semaphore + computed-key in-memory check above stay for this phase (belt-and-suspenders; removed WC4).
        var lifecycleCode = ServiceRequestMessageMapping.LifecycleCode(srType, srMsgType, content);
        var sourceKey = lifecycleCode is not null
            ? ServiceRequestMessageMapping.SystemSourceKey(m.ServiceRequestId, lifecycleCode)
            : ServiceRequestMessageMapping.SourceKey(m.ServiceRequestId, m.MessageId);
        var msg = ServiceRequestMessageMapping.MapMessage(
            conv.Id, m.SenderUserId, srType, srMsgType, content, m.AttachmentFileId, sentAt, senderName,
            m.LocationLat, m.LocationLng, m.LocationLabel, sourceKey);
        conv.AddMessage(msg);
        conv.MarkReadByAdmin();
        db.Conversations.Update(conv);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // A concurrent/redelivered insert already persisted this exact (ConversationId, SourceKey). Treat as a
            // benign no-op and suppress the republish so the admin realtime edge never double-fires. This is the
            // durable multi-replica guarantee the SemaphoreSlim only approximated on a single replica.
            _logger.LogDebug("[SR→Messaging live-sync] unique-violation (duplicate SourceKey) skipped SR {SrId}", m.ServiceRequestId);
            return;
        }

        // Persistent notifications: mirror the module's SendMessageCommandHandler — RecipientUserIds =
        // conversation participants MINUS the sender. Only REAL Owner/Provider chat messages notify; System /
        // lifecycle messages (JOB_STARTED, OFFER_ACCEPTED, …) and Admin messages stay silent to avoid notification
        // noise. The System pseudo-participant (UserId 0) is never a notification target. The dedupe-skip path
        // already returned above, so redelivery/backfill never double-notifies. Admin realtime is unaffected: the
        // admin socket mapper broadcasts on this same event regardless of RecipientUserIds (System messages publish
        // with empty recipients → Notification's consumer returns early, admin still sees them live).
        var recipientUserIds =
            m.SenderType is ServiceRequestMessageSenderType.Owner or ServiceRequestMessageSenderType.Provider
                ? conv.Participants
                    .Where(p => p.UserId != m.SenderUserId && p.UserId != 0)
                    .Select(p => p.UserId)
                    .Distinct()
                    .ToList()
                : new List<long>();

        await publisher.PublishAsync(new MessagingMessageSentMessage
        {
            ConversationId    = conv.Id,
            ConversationTitle = conv.Title,
            SenderUserId      = m.SenderUserId,
            SenderName        = senderName ?? ServiceRequestMessageMapping.RoleName((MessagingParticipantRole)srType),
            ContextType       = MessagingContextType.ServiceRequest,
            ContextId         = m.ServiceRequestId,
            RecipientUserIds  = recipientUserIds,
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

    // A Postgres unique-constraint violation (SQLSTATE 23505) surfaces as a DbUpdateException wrapping a
    // PostgresException. Detected via SqlState so we don't take a hard Npgsql type dependency in the walk.
    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        for (Exception? e = ex; e is not null; e = e.InnerException)
        {
            var sqlState = e.GetType().GetProperty("SqlState")?.GetValue(e) as string;
            if (sqlState == "23505") return true;
        }
        return false;
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
