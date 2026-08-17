using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Location.Queries;

[DocumentationInfo("Returns details for a single neighborhood", "Cached for 24 hours; invalidated on location changes.")]
public sealed class GetNeighborhoodDetailQueryHandler
    : AizenQueryHandler<GetNeighborhoodDetailQuery, NeighborhoodDto?>, IAizenQueryHandlerCacheable
{
    private readonly ILocationReferenceService _service;

    public GetNeighborhoodDetailQueryHandler(ILocationReferenceService service) => _service = service;

    public override Task<NeighborhoodDto?> Handle(GetNeighborhoodDetailQuery request, CancellationToken cancellationToken)
        => _service.GetNeighborhoodAsync(request.CountryCode, request.CityCode, request.DistrictCode, request.NeighborhoodCode, cancellationToken);

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) };
}
