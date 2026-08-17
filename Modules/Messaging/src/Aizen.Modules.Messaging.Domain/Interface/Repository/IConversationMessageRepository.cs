using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Domain.Entities.Conversation;

namespace Aizen.Modules.Messaging.Domain.Interface.Repository;

[DocumentationInfo("Conversation message repository interface", "Data access for messages within conversations.")]
public interface IConversationMessageRepository
{
    Task<IReadOnlyList<ConversationMessageEntity>> GetByConversationIdAsync(
        long conversationId,
        int skip,
        int take,
        CancellationToken ct = default);

    Task<ConversationMessageEntity?> GetByIdAsync(long id, CancellationToken ct = default);

    /// <summary>Returns flagged and pending-review messages for moderation queue.</summary>
    Task<IReadOnlyList<ConversationMessageEntity>> GetFlaggedAsync(
        int skip,
        int take,
        CancellationToken ct = default);

    Task AddAsync(ConversationMessageEntity entity, CancellationToken ct = default);
    void Update(ConversationMessageEntity entity);
}
