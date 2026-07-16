using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Provider;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.ServiceRequests;

public sealed class GetDiscoveryMarkersBffQueryHandler
    : AizenQueryHandler<GetDiscoveryMarkersBffQuery, ProviderDiscoveryMarkersResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _identityHolder;
    private readonly IProviderServiceRequestRemoteCall _serviceRequest;
    private readonly ILogger<GetDiscoveryMarkersBffQueryHandler> _logger;

    public GetDiscoveryMarkersBffQueryHandler(
        IProviderProfileResolver resolver,
        IProviderIdentityHolder identityHolder,
        IProviderServiceRequestRemoteCall serviceRequest,
        ILogger<GetDiscoveryMarkersBffQueryHandler> logger)
    {
        _resolver = resolver;
        _identityHolder = identityHolder;
        _serviceRequest = serviceRequest;
        _logger = logger;
    }

    public override async Task<ProviderDiscoveryMarkersResponse?> Handle(
        GetDiscoveryMarkersBffQuery request, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_identityHolder.ProfileId is null or 0)
        {
            _logger.LogWarning("Provider profile not resolved. Returning empty markers.");
            return new ProviderDiscoveryMarkersResponse();
        }

        var result = await _serviceRequest.GetDiscoveryMarkers(
            request.BoundsMinLat, request.BoundsMaxLat,
            request.BoundsMinLng, request.BoundsMaxLng,
            request.LocationCityCode, request.LocationCountryCode,
            request.ServiceCategoryCode, request.SearchTerm);

        return result.Body;
    }
}
