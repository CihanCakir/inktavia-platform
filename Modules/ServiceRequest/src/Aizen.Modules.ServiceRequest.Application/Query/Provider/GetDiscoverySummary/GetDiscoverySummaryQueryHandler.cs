using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Provider;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

namespace Aizen.Modules.ServiceRequest.Application.Query.Provider.GetDiscoverySummary;

[DocumentationInfo("Get discovery summary handler", "Returns aggregate counts for biddable service requests matching the discovery filter.")]
public sealed class GetDiscoverySummaryQueryHandler
    : AizenQueryHandler<GetDiscoverySummaryQuery, ProviderDiscoverySummaryResponse>
{
    private readonly IServiceRequestRepository _repository;
    private readonly IAizenInfoAccessor _info;

    public GetDiscoverySummaryQueryHandler(
        IServiceRequestRepository repository,
        IAizenInfoAccessor info)
    {
        _repository = repository;
        _info = info;
    }

    public override async Task<ProviderDiscoverySummaryResponse?> Handle(
        GetDiscoverySummaryQuery request, CancellationToken ct)
    {
        var providerProfileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
        if (providerProfileId <= 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        var filter = request.Filter;

        var isGeoMode = filter.CenterLatitude.HasValue || filter.BoundsMinLat.HasValue;

        var summary = await _repository.GetDiscoverySummaryAsync(providerProfileId, filter, ct);

        return new ProviderDiscoverySummaryResponse
        {
            OpenCount = summary.OpenCount,
            PublishedTodayCount = summary.PublishedTodayCount,
            EmergencyCount = summary.EmergencyCount,
            MyActiveOfferCount = summary.MyActiveOfferCount,
            LocationMode = isGeoMode ? "Geo" : "City",
        };
    }
}
