using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Dto.Media;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Application.Mapping;
using Aizen.Modules.Vessel.Domain.Interface.Repository;

namespace Aizen.Modules.Vessel.Application.Query.Media;

[DocumentationInfo("Get Vessel Media Query Handler", "Returns all media items for a vessel ordered by sort order; cached for 15 minutes.")]
public sealed class GetVesselMediaQueryHandler : AizenQueryHandler<GetVesselMediaQuery, IReadOnlyList<VesselMediaDto>>, IAizenQueryHandlerCacheable
{
    private readonly IVesselMediaRepository _mediaRepository;

    public GetVesselMediaQueryHandler(IVesselMediaRepository mediaRepository)
    {
        _mediaRepository = mediaRepository;
    }

    public override async Task<IReadOnlyList<VesselMediaDto>> Handle(GetVesselMediaQuery request, CancellationToken cancellationToken)
    {
        var media = await _mediaRepository.GetByVesselIdAsync(request.VesselId, cancellationToken);
        return media.OrderBy(m => m.SortOrder).Select(m => m.ToDto()).ToList().AsReadOnly();
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) };
}
