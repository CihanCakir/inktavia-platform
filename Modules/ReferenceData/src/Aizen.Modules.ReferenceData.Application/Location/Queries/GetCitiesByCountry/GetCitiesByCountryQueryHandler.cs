using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;
using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Model;

namespace Aizen.Modules.ReferenceData.Application.Location.Queries;

[DocumentationInfo("Returns cities for a given country code", "Cached for 24 hours; invalidated on location changes.")]
public sealed class GetCitiesByCountryQueryHandler : AizenQueryHandler<GetCitiesByCountryQuery, IReadOnlyList<CityDto>>, IAizenQueryHandlerCacheable
{
    private readonly ILocationReferenceService _service;

    public GetCitiesByCountryQueryHandler(ILocationReferenceService service)
    {
        _service = service;
    }

    public override async Task<IReadOnlyList<CityDto>> Handle(GetCitiesByCountryQuery request, CancellationToken cancellationToken)
    {
        return await _service.GetCitiesByCountryAsync(request.CountryCode, request.OnlyActive, cancellationToken);
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) };
}
