using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;
using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Model;

namespace Aizen.Modules.ReferenceData.Application.Location.Queries;

[DocumentationInfo("Returns neighborhoods for a given country, city, and district code", "Cached for 24 hours; invalidated on location changes.")]
public sealed class GetNeighborhoodsByDistrictQueryHandler : AizenQueryHandler<GetNeighborhoodsByDistrictQuery, IReadOnlyList<NeighborhoodDto>>, IAizenQueryHandlerCacheable
{
    private readonly ILocationReferenceService _service;

    public GetNeighborhoodsByDistrictQueryHandler(ILocationReferenceService service)
    {
        _service = service;
    }

    public override async Task<IReadOnlyList<NeighborhoodDto>> Handle(GetNeighborhoodsByDistrictQuery request, CancellationToken cancellationToken)
    {
        return await _service.GetNeighborhoodsByDistrictAsync(request.CountryCode, request.CityCode, request.DistrictCode, request.OnlyActive, cancellationToken);
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) };
}
