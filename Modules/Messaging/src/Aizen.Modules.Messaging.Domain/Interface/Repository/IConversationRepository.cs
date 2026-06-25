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
