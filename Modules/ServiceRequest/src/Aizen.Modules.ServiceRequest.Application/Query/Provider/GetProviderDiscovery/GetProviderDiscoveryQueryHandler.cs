using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.RemoteCall;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Provider;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

namespace Aizen.Modules.ServiceRequest.Application.Query.Provider.GetProviderDiscovery;

[DocumentationInfo("Get provider discovery handler", "Returns paginated discovery list of biddable service requests with cursor pagination and projection.")]
public sealed class GetProviderDiscoveryQueryHandler
    : AizenQueryHandler<GetProviderDiscoveryQuery, ProviderDiscoveryResponse>
{
    private readonly IServiceRequestRepository _repository;
    private readonly IAizenInfoAccessor _info;
    private readonly IServiceRequestReferenceDataRemoteCall _referenceData;

    public GetProviderDiscoveryQueryHandler(
        IServiceRequestRepository repository,
        IAizenInfoAccessor info,
        IServiceRequestReferenceDataRemoteCall referenceData)
    {
        _repository = repository;
        _info = info;
        _referenceData = referenceData;
    }

    public override async Task<ProviderDiscoveryResponse?> Handle(
        GetProviderDiscoveryQuery request, CancellationToken ct)
    {
        var providerProfileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
        if (providerProfileId <= 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        var filter = request.Filter;

        // PageSize cap
        if (filter.PageSize > 50)
            throw new AizenBusinessException("PageSize must not exceed 50.");

        // Geo validation
        if (filter.CenterLatitude.HasValue || filter.CenterLongitude.HasValue || filter.RadiusKm.HasValue)
        {
            if (!filter.CenterLatitude.HasValue || !filter.CenterLongitude.HasValue || !filter.RadiusKm.HasValue)
                throw new AizenBusinessException("CenterLatitude, CenterLongitude, and RadiusKm must all be provided together.");
            if (filter.CenterLatitude.Value < -90 || filter.CenterLatitude.Value > 90)
                throw new AizenBusinessException("CenterLatitude must be between -90 and 90.");
            if (filter.CenterLongitude.Value < -180 || filter.CenterLongitude.Value > 180)
                throw new AizenBusinessException("CenterLongitude must be between -180 and 180.");
            if (filter.RadiusKm.Value <= 0 || filter.RadiusKm.Value > 200)
                throw new AizenBusinessException("RadiusKm must be between 0 (exclusive) and 200.");
        }

        if (filter.BoundsMinLat.HasValue || filter.BoundsMaxLat.HasValue ||
            filter.BoundsMinLng.HasValue || filter.BoundsMaxLng.HasValue)
        {
            if (!filter.BoundsMinLat.HasValue || !filter.BoundsMaxLat.HasValue ||
                !filter.BoundsMinLng.HasValue || !filter.BoundsMaxLng.HasValue)
                throw new AizenBusinessException("All four Bounds fields must be provided together.");
            if (filter.BoundsMinLat.Value >= filter.BoundsMaxLat.Value)
                throw new AizenBusinessException("BoundsMinLat must be less than BoundsMaxLat.");
            if (filter.BoundsMinLng.Value >= filter.BoundsMaxLng.Value)
                throw new AizenBusinessException("BoundsMinLng must be less than BoundsMaxLng.");
        }

        if (filter.SortBy == "DistanceAsc" && !filter.CenterLatitude.HasValue)
            throw new AizenBusinessException("DistanceAsc sort requires CenterLatitude and CenterLongitude.");

        // Validate LocationCityCode against ReferenceData — unknown code is rejected, not silently ignored.
        if (!string.IsNullOrWhiteSpace(filter.LocationCityCode))
        {
            var country = filter.LocationCountryCode ?? "TR";
            try
            {
                var cityResult = await _referenceData.GetCity(country, filter.LocationCityCode);
                if (cityResult.Body is null || !cityResult.Body.IsActive)
                    throw new AizenBusinessException($"Location city code '{filter.LocationCityCode}' is not a recognised ReferenceData city.");
            }
            catch (AizenBusinessException) { throw; }
            catch (Exception)
            {
                throw new AizenBusinessException($"Could not validate city code '{filter.LocationCityCode}'. ReferenceData is unreachable.");
            }
        }

        var pageSize = Math.Clamp(filter.PageSize, 1, 50);

        var items = await _repository.GetDiscoveryAsync(providerProfileId, filter, ct);

        string? nextCursor = null;
        if (items.Count > pageSize)
        {
            var lastItem = items[pageSize - 1];
            items = items.Take(pageSize).ToList();
            nextCursor = CursorHelper.Encode(lastItem, filter);
        }

        // Coordinates are snapped inside the SQL projection (repository) — exact coordinates
        // never appear in ProviderDiscoveryItemDto. No post-processing needed here.

        var isGeoMode = filter.CenterLatitude.HasValue || filter.BoundsMinLat.HasValue;

        return new ProviderDiscoveryResponse
        {
            Items = items,
            NextCursor = nextCursor,
            PageSize = pageSize,
            LocationMode = isGeoMode ? "Geo" : "City",
        };
    }
}
