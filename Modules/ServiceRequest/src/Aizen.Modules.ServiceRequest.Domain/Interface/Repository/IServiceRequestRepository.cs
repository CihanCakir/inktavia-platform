using Aizen.Core.Domain;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Filter;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Admin;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Provider;
using Aizen.Modules.ServiceRequest.Domain.Entities.Pricing;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.ReadModel;

namespace Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

[DocumentationInfo("Service request repository interface", "Data access contract for the ServiceRequest aggregate root.")]
public interface IServiceRequestRepository
{
    Task<ServiceRequestEntity?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<ServiceRequestEntity?> GetByIdWithDetailsAsync(long id, CancellationToken ct = default);
    Task<ServiceRequestEntity?> GetByCodeAsync(string requestCode, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceRequestEntity>> GetByOwnerUserIdAsync(long ownerUserId, int skip, int take, CancellationToken ct = default);
    Task<int> CountByOwnerUserIdAsync(long ownerUserId, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceRequestEntity>> GetByVesselIdAsync(long vesselId, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceRequestEntity>> GetAdminListAsync(AdminServiceRequestFilterRequest filter, CancellationToken ct = default);
    Task<int> CountAdminAsync(AdminServiceRequestFilterRequest filter, CancellationToken ct = default);
    Task<GetAdminServiceRequestStatsResponse> GetAdminStatsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ServiceRequestStatusCount>> GetStatusBreakdownAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ServiceRequestEntity>> GetOpenForProviderAsync(long providerProfileId, ProviderAvailableServiceRequestFilterRequest filter, CancellationToken ct = default);
    Task<int> CountOpenForProviderAsync(long providerProfileId, ProviderAvailableServiceRequestFilterRequest filter, CancellationToken ct = default);
    Task<List<ProviderDiscoveryItemDto>> GetDiscoveryAsync(long providerProfileId, ProviderServiceRequestDiscoveryFilter filter, CancellationToken ct = default);
    Task<List<DiscoveryMarkerDto>> GetDiscoveryMarkersAsync(long providerProfileId, ProviderServiceRequestDiscoveryFilter filter, CancellationToken ct = default);
    Task<ProviderDiscoverySummaryResponse> GetDiscoverySummaryAsync(long providerProfileId, ProviderServiceRequestDiscoveryFilter filter, CancellationToken ct = default);
    Task AddAsync(ServiceRequestEntity entity, CancellationToken ct = default);
    void Update(ServiceRequestEntity entity);

    /// <summary>BE-S4 — travel-pricing details keyed by offer-item id, for the given offer items (Travel lines). Read-only.</summary>
    Task<IReadOnlyDictionary<long, TravelPricingDetailEntity>> GetTravelPricingByOfferItemIdsAsync(
        IReadOnlyCollection<long> offerItemIds, CancellationToken ct = default);
}
