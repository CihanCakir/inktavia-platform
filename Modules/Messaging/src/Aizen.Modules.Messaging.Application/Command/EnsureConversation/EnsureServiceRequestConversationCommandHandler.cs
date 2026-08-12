using System.Data;
using System.Data.Common;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;
using Aizen.Modules.Messaging.Domain.Entities.Conversation;
using Aizen.Modules.Messaging.Repository.Persistence;
using Aizen.Modules.Messaging.Repository.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Messaging.Application.Command.EnsureConversation;

/// <summary>
/// BE_WC4a — resolves the SR owner + accepted provider from the SR schema (read-only raw SQL over the shared
/// inktavia_store DB, exactly as the sync consumer / lifecycle writer do — Messaging has no SR remote-call) and
/// idempotently get-or-creates the ServiceRequest conversation. Idempotency is the WC0 unique index on
/// (ContextType, ContextId): a concurrent create that loses the race is caught as a 23505 and re-read.
/// </summary>
[DocumentationInfo("Ensure ServiceRequest conversation command handler",
    "Idempotent get-or-create of the SR conversation with server-resolved owner + accepted-provider participants (WC4a).")]
public sealed class EnsureServiceRequestConversationCommandHandler
    : AizenCommandHandler<EnsureServiceRequestConversationCommand, EnsureConversationByContextResponse>
{
    private readonly MessagingDbContext _db;
    private readonly MessagingUserNameResolver _names;
    private readonly ILogger<EnsureServiceRequestConversationCommandHandler> _logger;

    public EnsureServiceRequestConversationCommandHandler(
        MessagingDbContext db, MessagingUserNameResolver names,
        ILogger<EnsureServiceRequestConversationCommandHandler> logger)
    {
        _db = db;
        _names = names;
        _logger = logger;
    }

    public override async Task<EnsureConversationByContextResponse?> Handle(
        EnsureServiceRequestConversationCommand request, CancellationToken cancellationToken)
    {
        var srId = request.ServiceRequestId;

        // Fast path: the conversation already exists → return it untouched (participants NOT re-resolved).
        var existing = await _db.Conversations.AsNoTracking().FirstOrDefaultAsync(
            c => c.ContextType == MessagingContextType.ServiceRequest && c.ContextId == srId && !c.IsDeleted,
            cancellationToken);
        if (existing is not null)
            return new EnsureConversationByContextResponse { ConversationId = existing.Id, Created = false };

        // Resolve participants server-side from the SR schema (owner always; accepted provider iff one exists yet).
        var (ownerUserId, title) = await ReadServiceRequestAsync(srId, cancellationToken);
        if (ownerUserId is null)
        {
            _logger.LogWarning("[WC4a ensure] SR {SrId} not found — no conversation created", srId);
            return new EnsureConversationByContextResponse { ConversationId = 0, Created = false };
        }
        var providerUserId = await ReadAcceptedProviderUserIdAsync(srId, cancellationToken);

        var idsToName = new List<long> { ownerUserId.Value };
        if (providerUserId is { } pu) idsToName.Add(pu);
        var nameMap = await _names.ResolveAsync(idsToName, cancellationToken);
        var ownerName = nameMap.TryGetValue(ownerUserId.Value, out var on) ? on : "Owner";
        var providerName = providerUserId is { } pp && nameMap.TryGetValue(pp, out var pn) ? pn : "Provider";

        var convTitle = string.IsNullOrWhiteSpace(title) ? $"Service Request #{srId}" : title;
        var conv = ConversationEntity.Create(MessagingContextType.ServiceRequest, srId, convTitle);
        conv.AddParticipant(ConversationParticipantEntity.Create(
            conv.Id, ownerUserId.Value, ownerName, MessagingParticipantRole.Owner));
        if (providerUserId is { } p2)
            conv.AddParticipant(ConversationParticipantEntity.Create(
                conv.Id, p2, providerName, MessagingParticipantRole.Provider));

        await _db.Conversations.AddAsync(conv, cancellationToken);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("[WC4a ensure] created conversation {ConvId} for SR {SrId}", conv.Id, srId);
            return new EnsureConversationByContextResponse { ConversationId = conv.Id, Created = true };
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // A concurrent first-message created it first — the WC0 unique index rejected this insert. Re-read the winner.
            _db.ChangeTracker.Clear();
            var raced = await _db.Conversations.AsNoTracking().FirstOrDefaultAsync(
                c => c.ContextType == MessagingContextType.ServiceRequest && c.ContextId == srId, cancellationToken);
            _logger.LogDebug("[WC4a ensure] unique-violation for SR {SrId} → returning existing conv {ConvId}",
                srId, raced?.Id);
            return new EnsureConversationByContextResponse { ConversationId = raced?.Id ?? 0, Created = false };
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        for (Exception? e = ex; e is not null; e = e.InnerException)
            if (e.GetType().GetProperty("SqlState")?.GetValue(e) as string == "23505") return true;
        return false;
    }

    // ── read-only SR reads via raw SQL over the shared inktavia_store DB — mirrors ServiceRequestLifecycleMessageWriter ──
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

    private static void AddParam(DbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }
}
