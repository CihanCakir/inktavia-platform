using Aizen.Modules.ServiceRequest.Domain.Entities.Conversation;

namespace Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

public interface IServiceRequestConversationRepository
{
    Task<IReadOnlyList<ServiceRequestConversationEntity>> GetListAsync(string? filter, CancellationToken ct = default);
    Task<ServiceRequestConversationEntity?> GetByIdWithMessagesAsync(long id, CancellationToken ct = default);
    Task<ServiceRequestConversationEntity?> GetByIdAsync(long id, CancellationToken ct = default);
    Task AddAsync(ServiceRequestConversationEntity entity, CancellationToken ct = default);
    void Update(ServiceRequestConversationEntity entity);
}
