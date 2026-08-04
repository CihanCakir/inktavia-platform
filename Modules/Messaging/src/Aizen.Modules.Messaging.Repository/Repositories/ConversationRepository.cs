using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Domain.Entities.Conversation;
using Aizen.Modules.Messaging.Domain.Interface.Repository;
using Aizen.Modules.Messaging.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Messaging.Repository.Repositories;

[DocumentationInfo("Conversation repository", "EF Core implementation of IConversationRepository.")]
public sealed class ConversationRepository : IConversationRepository
{
    private readonly MessagingDbContext _db;
    public ConversationRepository(MessagingDbContext db) => _db = db;

    public async Task<IReadOnlyList<ConversationEntity>> GetListAsync(
        ConversationStatus? status,
        MessagingContextType? contextType,
        int skip, int take,
        CancellationToken ct = default)
    {
        var query = _db.Conversations
            .AsNoTracking()
            .Include(x => x.Participants)
            .Where(x => !x.IsDeleted);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (contextType.HasValue)
            query = query.Where(x => x.ContextType == contextType.Value);

        return await query
            .OrderByDescending(x => x.LastMessageAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
    }

    // ── PARTICIPANT-SCOPED reads (Phase 3) — mirror GetListAsync but add a participant filter so a caller sees
    // only its own conversations. The admin unscoped GetListAsync/CountAsync are deliberately left untouched. ──

    public async Task<IReadOnlyList<ConversationEntity>> GetListForParticipantAsync(
        long userId,
        MessagingContextType? contextType,
        int skip, int take,
        CancellationToken ct = default)
    {
        var query = _db.Conversations
            .AsNoTracking()
            .Include(x => x.Participants)
            .Where(x => !x.IsDeleted)
            .Where(x => x.Participants.Any(p => p.UserId == userId));

        if (contextType.HasValue)
            query = query.Where(x => x.ContextType == contextType.Value);

        return await query
            .OrderByDescending(x => x.LastMessageAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
    }

    public Task<int> CountForParticipantAsync(
        long userId, MessagingContextType? contextType, CancellationToken ct = default)
    {
        var query = _db.Conversations
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Where(x => x.Participants.Any(p => p.UserId == userId));

        if (contextType.HasValue)
            query = query.Where(x => x.ContextType == contextType.Value);

        return query.CountAsync(ct);
    }

    public Task<ConversationEntity?> GetByContextWithMessagesAsync(
        MessagingContextType contextType, long contextId, CancellationToken ct = default)
        => _db.Conversations
            .Include(x => x.Participants)
            .Include(x => x.Messages)
                .ThenInclude(m => m.Attachments)
            .FirstOrDefaultAsync(x =>
                x.ContextType == contextType &&
                x.ContextId   == contextId   &&
                !x.IsDeleted, ct);

    public Task<ConversationEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => _db.Conversations
            .Include(x => x.Participants)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public Task<ConversationEntity?> GetByIdWithMessagesAsync(long id, CancellationToken ct = default)
        => _db.Conversations
            .Include(x => x.Participants)
            .Include(x => x.Messages)
                .ThenInclude(m => m.Attachments)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public Task<ConversationEntity?> GetByContextAsync(
        MessagingContextType contextType, long contextId, CancellationToken ct = default)
        => _db.Conversations
            .Include(x => x.Participants)
            .FirstOrDefaultAsync(x =>
                x.ContextType == contextType &&
                x.ContextId   == contextId   &&
                !x.IsDeleted, ct);

    public Task<int> CountAsync(
        ConversationStatus? status, MessagingContextType? contextType, CancellationToken ct = default)
    {
        var query = _db.Conversations.AsNoTracking().Where(x => !x.IsDeleted);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        if (contextType.HasValue) query = query.Where(x => x.ContextType == contextType.Value);
        return query.CountAsync(ct);
    }

    public Task AddAsync(ConversationEntity entity, CancellationToken ct = default)
        => _db.Conversations.AddAsync(entity, ct).AsTask();

    public void Update(ConversationEntity entity)
        => _db.Conversations.Update(entity);
}
