using Aizen.Core.Domain;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;

namespace Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

[DocumentationInfo("Service request message repository interface", "Data access contract for ServiceRequestMessage entities.")]
public interface IServiceRequestMessageRepository
{
    Task<IReadOnlyList<ServiceRequestMessageEntity>> GetByServiceRequestIdAsync(long serviceRequestId, int skip, int take, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(long serviceRequestId, long recipientUserId, CancellationToken ct = default);
    Task<bool> HasOwnerMessageAsync(long serviceRequestId, CancellationToken ct = default);
    Task<bool> HasOfferMessageForOfferAsync(long serviceRequestId, long offerId, CancellationToken ct = default);
    Task<bool> HasSystemMessageAsync(long serviceRequestId, string statusCode, CancellationToken ct = default);
    Task AddAsync(ServiceRequestMessageEntity entity, CancellationToken ct = default);
    void Update(ServiceRequestMessageEntity entity);
}
