using Aizen.Core.Domain;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Domain.Entities.Offer;

namespace Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

[DocumentationInfo("Service request offer repository interface", "Data access contract for ServiceRequestOffer entities.")]
public interface IServiceRequestOfferRepository
{
    Task<ServiceRequestOfferEntity?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceRequestOfferEntity>> GetByServiceRequestIdAsync(long serviceRequestId, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceRequestOfferEntity>> GetByProviderProfileIdAsync(long providerProfileId, int skip, int take, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceRequestOfferEntity>> GetByProviderProfileIdWithSrAsync(long providerProfileId, ServiceRequestOfferStatus? statusFilter, int skip, int take, CancellationToken ct = default);
    Task<ServiceRequestOfferEntity?> GetDraftByProviderAndRequestAsync(long providerProfileId, long serviceRequestId, CancellationToken ct = default);
    Task<ServiceRequestOfferEntity?> GetByProviderRequestAndIdempotencyAsync(long providerProfileId, long serviceRequestId, CancellationToken ct = default);
    Task AddAsync(ServiceRequestOfferEntity entity, CancellationToken ct = default);
    void Update(ServiceRequestOfferEntity entity);
}
