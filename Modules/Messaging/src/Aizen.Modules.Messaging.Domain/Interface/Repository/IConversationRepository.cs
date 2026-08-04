using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Domain.Entities.Conversation;

namespace Aizen.Modules.Messaging.Domain.Interface.Repository;

[DocumentationInfo("Conversation repository interface", "Data access contract for the Conversation aggregate.")]
public interface IConversationRepository
{
    Task<IReadOnlyList<ConversationEntity>> GetListAsync(
        ConversationStatus? status,
        MessagingContextType? contextType,
        int skip,
        int take,
        CancellationToken ct = default);

    // ── PARTICIPANT-SCOPED reads (Phase 3 provider read cutover) — the caller sees ONLY conversations it
    // participates in. Additive; the admin unscoped GetListAsync/CountAsync above are untouched. ──

    /// <summary>Conversations where <paramref name="userId"/> is a participant, newest-message first.</summary>
    Task<IReadOnlyList<ConversationEntity>> GetListForParticipantAsync(
        long userId,
        MessagingContextType? contextType,
        int skip,
        int take,
        CancellationToken ct = default);

    /// <summary>Total conversations where <paramref name="userId"/> is a participant (for pagination).</summary>
    Task<int> CountForParticipantAsync(
        long userId, MessagingContextType? contextType, CancellationToken ct = default);

    /// <summary>By-context lookup that ALSO loads Messages (+ attachments) — the thread read for participant apps.</summary>
    Task<ConversationEntity?> GetByContextWithMessagesAsync(
        MessagingContextType contextType, long contextId, CancellationToken ct = default);

    Task<ConversationEntity?> GetByIdAsync(long id, CancellationToken ct = default);

    Task<ConversationEntity?> GetByIdWithMessagesAsync(long id, CancellationToken ct = default);

    Task<ConversationEntity?> GetByContextAsync(
        MessagingContextType contextType,
        long contextId,
        CancellationToken ct = default);

    Task<int> CountAsync(ConversationStatus? status, MessagingContextType? contextType, CancellationToken ct = default);

    Task AddAsync(ConversationEntity entity, CancellationToken ct = default);
    void Update(ConversationEntity entity);
}
