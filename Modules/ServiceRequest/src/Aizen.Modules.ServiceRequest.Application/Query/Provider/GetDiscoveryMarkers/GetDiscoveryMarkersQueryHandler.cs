using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Provider;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

namespace Aizen.Modules.ServiceRequest.Application.Query.Provider.GetDiscoveryMarkers;

[DocumentationInfo("Get discovery markers handler", "Returns map markers for biddable service requests within the given bounds.")]
public sealed class GetDiscoveryMarkersQueryHandler
    : AizenQueryHandler<GetDiscoveryMarkersQuery, ProviderDiscoveryMarkersResponse>
{
    private readonly IServiceRequestRepository _repository;
    private readonly IAizenInfoAccessor _info;

    public GetDiscoveryMarkersQueryHandler(
        IServiceRequestRepository repository,
        IAizenInfoAccessor info)
    {
        _repository = repository;
        _info = info;
    }

    public override async Task<ProviderDiscoveryMarkersResponse?> Handle(
        GetDiscoveryMarkersQuery request, CancellationToken ct)
    {
        var providerProfileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
        if (providerProfileId <= 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        var filter = request.Filter;

        // Bounds are required for markers
        if (!filter.BoundsMinLat.HasValue || !filter.BoundsMaxLat.HasValue ||
            !filter.BoundsMinLng.HasValue || !filter.BoundsMaxLng.HasValue)
            throw new AizenBusinessException("All four Bounds fields are required for markers.");

        if (filter.BoundsMinLat.Value >= filter.BoundsMaxLat.Value)
            throw new AizenBusinessException("BoundsMinLat must be less than BoundsMaxLat.");
        if (filter.BoundsMinLng.Value >= filter.BoundsMaxLng.Value)
            throw new AizenBusinessException("BoundsMinLng must be less than BoundsMaxLng.");

        var markers = await _repository.GetDiscoveryMarkersAsync(providerProfileId, filter, ct);

        var truncated = markers.Count > 500;
        if (truncated)
            markers = markers.Take(500).ToList();

        return new ProviderDiscoveryMarkersResponse
        {
            Markers = markers,
            Truncated = truncated,
        };
    }
}
