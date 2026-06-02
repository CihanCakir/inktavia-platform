using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Application.Mapping;
using Aizen.Modules.Vessel.Domain.Interface.Repository;

namespace Aizen.Modules.Vessel.Application.Query.Vessel;

[DocumentationInfo("Get User Vessels Query Handler", "Returns a list of vessels owned by the user; cached for 10 minutes.")]
public sealed class GetUserVesselsQueryHandler : AizenQueryHandler<GetUserVesselsQuery, IReadOnlyList<VesselListItemDto>>, IAizenQueryHandlerCacheable
{
    private readonly IVesselRepository _vesselRepository;

    public GetUserVesselsQueryHandler(IVesselRepository vesselRepository)
    {
        _vesselRepository = vesselRepository;
    }

    public override async Task<IReadOnlyList<VesselListItemDto>> Handle(GetUserVesselsQuery request, CancellationToken cancellationToken)
    {
        var vessels = await _vesselRepository.GetByOwnerUserIdAsync(request.UserId, cancellationToken);
        return vessels.Select(v => v.ToListItemDto()).ToList().AsReadOnly();
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) };
}
