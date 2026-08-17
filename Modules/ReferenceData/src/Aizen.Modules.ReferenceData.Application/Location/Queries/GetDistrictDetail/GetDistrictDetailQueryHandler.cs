using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Location.Queries;

[DocumentationInfo("Returns details for a single district", "Cached for 24 hours; invalidated on location changes.")]
public sealed class GetDistrictDetailQueryHandler
    : AizenQueryHandler<GetDistrictDetailQuery, DistrictDto?>, IAizenQueryHandlerCacheable
{
    private readonly ILocationReferenceService _service;

    public GetDistrictDetailQueryHandler(ILocationReferenceService service) => _service = service;

    public override Task<DistrictDto?> Handle(GetDistrictDetailQuery request, CancellationToken cancellationToken)
        => _service.GetDistrictAsync(request.CountryCode, request.CityCode, request.DistrictCode, cancellationToken);

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) };
}
