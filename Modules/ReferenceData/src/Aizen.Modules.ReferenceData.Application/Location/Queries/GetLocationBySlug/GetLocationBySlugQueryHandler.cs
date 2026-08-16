using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Location.Queries;

[DocumentationInfo("Resolves a location by its flat slug", "Cached for 24 hours; invalidated on location changes.")]
public sealed class GetLocationBySlugQueryHandler
    : AizenQueryHandler<GetLocationBySlugQuery, LocationBySlugDto?>, IAizenQueryHandlerCacheable
{
    private readonly ILocationReferenceService _service;

    public GetLocationBySlugQueryHandler(ILocationReferenceService service) => _service = service;

    public override Task<LocationBySlugDto?> Handle(GetLocationBySlugQuery request, CancellationToken cancellationToken)
        => _service.ResolveBySlugAsync(request.Slug, cancellationToken);

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) };
}
