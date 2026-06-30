using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;
using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;

namespace Aizen.Modules.ReferenceData.Application.Location.Queries;

[DocumentationInfo("Returns districts for a given country and city code", "Cached for 24 hours; invalidated on location changes.")]
public sealed class GetDistrictsByCityQueryHandler : AizenQueryHandler<GetDistrictsByCityQuery, IReadOnlyList<DistrictDto>>, IAizenQueryHandlerCacheable
{
    private readonly ILocationReferenceService _service;

    public GetDistrictsByCityQueryHandler(ILocationReferenceService service)
    {
        _service = service;
    }

    public override async Task<IReadOnlyList<DistrictDto>> Handle(GetDistrictsByCityQuery request, CancellationToken cancellationToken)
    {
        return await _service.GetDistrictsByCityAsync(request.CountryCode, request.CityCode, request.OnlyActive, cancellationToken);
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) };
}
