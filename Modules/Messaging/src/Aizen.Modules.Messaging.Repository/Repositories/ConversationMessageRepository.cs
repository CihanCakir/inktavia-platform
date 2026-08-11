using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Domain.Entities.Conversation;
using Aizen.Modules.Messaging.Domain.Interface.Repository;
using Aizen.Modules.Messaging.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Messaging.Repository.Repositories;

[DocumentationInfo("Conversation message repository", "EF Core implementation of IConversationMessageRepository.")]
public sealed class ConversationMessageRepository : IConversationMessageRepository
{
    private readonly MessagingDbContext _db;
    public ConversationMessageRepository(MessagingDbContext db) => _db = db;

    public async Task<IReadOnlyList<ConversationMessageEntity>> GetByConversationIdAsync(
        long conversationId, int skip, int take, CancellationToken ct = default)
        => await _db.ConversationMessages
            .AsNoTracking()
            .Include(x => x.Attachments)
            .Where(x => x.ConversationId == conversationId && !x.IsDeleted)
            .OrderBy(x => x.SentAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

    public Task<ConversationMessageEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => _db.ConversationMessages
            .Include(x => x.Attachments)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    // BE_WC2 anti-harassment gate — an Owner-role message means the owner has opened the channel. Owner role (1) is
    // distinct from System (4), so this inherently excludes WC1 System/lifecycle rows. Mirrors SR.HasOwnerMessageAsync.
    public Task<bool> HasOwnerMessageAsync(long conversationId, CancellationToken ct = default)
        => _db.ConversationMessages.AnyAsync(
            x => x.ConversationId == conversationId
                 && x.SenderRole == MessagingParticipantRole.Owner
                 && !x.IsDeleted, ct);

    public async Task<IReadOnlyList<ConversationMessageEntity>> GetFlaggedAsync(
        int skip, int take, CancellationToken ct = default)
        => await _db.ConversationMessages
            .AsNoTracking()
            .Include(x => x.Attachments)
            .Where(x => (x.ModerationStatus == MessageModerationStatus.Flagged
                       || x.ModerationStatus == MessageModerationStatus.PendingReview)
                      && !x.IsDeleted)
            .OrderByDescending(x => x.SentAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

    public Task AddAsync(ConversationMessageEntity entity, CancellationToken ct = default)
        => _db.ConversationMessages.AddAsync(entity, ct).AsTask();

    public void Update(ConversationMessageEntity entity)
        => _db.ConversationMessages.Update(entity);
}
