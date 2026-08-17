using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Repository.Persistence;
using Aizen.Modules.Messaging.Repository.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Messaging.Repository.Seed;

/// <summary>
/// PHASE 2 (Part C) one-off, idempotent pass that replaces the role-placeholder display names the backfill created
/// ("Owner"/"Provider"/…) with real names resolved from <c>public."UserProfiles"</c>, on both participants and message
/// sender names of ServiceRequest conversations — so existing migrated threads show real names in the admin audit.
/// Only rows whose name actually differs are updated (converges; safe to run every boot).
/// </summary>
public sealed class ServiceRequestChatNameFixer
{
    private readonly MessagingDbContext _db;
    private readonly MessagingUserNameResolver _names;
    private readonly ILogger<ServiceRequestChatNameFixer> _logger;

    public ServiceRequestChatNameFixer(MessagingDbContext db, MessagingUserNameResolver names, ILogger<ServiceRequestChatNameFixer> logger)
    {
        _db = db;
        _names = names;
        _logger = logger;
    }

    public async Task FixAsync(CancellationToken ct = default)
    {
        var srConvIds = await _db.Conversations
            .Where(c => c.ContextType == MessagingContextType.ServiceRequest)
            .Select(c => c.Id)
            .ToListAsync(ct);
        if (srConvIds.Count == 0) return;

        var participants = await _db.ConversationParticipants
            .Where(p => srConvIds.Contains(p.ConversationId))
            .Select(p => new { p.Id, p.UserId, p.DisplayName })
            .ToListAsync(ct);
        var messages = await _db.ConversationMessages
            .Where(m => srConvIds.Contains(m.ConversationId) && m.SenderUserId > 0)
            .Select(m => new { m.Id, m.SenderUserId, m.SenderName })
            .ToListAsync(ct);

        var userIds = participants.Select(p => p.UserId).Concat(messages.Select(m => m.SenderUserId)).Distinct();
        var nameMap = await _names.ResolveAsync(userIds, ct);
        if (nameMap.Count == 0) return;

        var fixedParticipants = 0;
        foreach (var p in participants)
        {
            if (nameMap.TryGetValue(p.UserId, out var name) && !string.Equals(p.DisplayName, name, StringComparison.Ordinal))
            {
                await _db.Database.ExecuteSqlRawAsync(
                    "UPDATE messaging.conversation_participants SET \"DisplayName\" = {0} WHERE \"Id\" = {1}", new object[] { name, p.Id }, ct);
                fixedParticipants++;
            }
        }

        var fixedMessages = 0;
        foreach (var m in messages)
        {
            if (nameMap.TryGetValue(m.SenderUserId, out var name) && !string.Equals(m.SenderName, name, StringComparison.Ordinal))
            {
                await _db.Database.ExecuteSqlRawAsync(
                    "UPDATE messaging.conversation_messages SET \"SenderName\" = {0} WHERE \"Id\" = {1}", new object[] { name, m.Id }, ct);
                fixedMessages++;
            }
        }

        if (fixedParticipants > 0 || fixedMessages > 0)
            _logger.LogInformation("[SR→Messaging name-fix] updated {Parts} participant + {Msgs} message names to real names",
                fixedParticipants, fixedMessages);
    }
}
