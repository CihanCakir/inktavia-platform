using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Dto.Location;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Application.Mapping;
using Aizen.Modules.Vessel.Domain.Interface.Repository;

namespace Aizen.Modules.Vessel.Application.Query.Location;

[DocumentationInfo("Get Current Vessel Location Query Handler", "Returns the current location snapshot for a vessel; cached for 5 minutes.")]
public sealed class GetCurrentVesselLocationQueryHandler : AizenQueryHandler<GetCurrentVesselLocationQuery, VesselLocationSnapshotDto?>, IAizenQueryHandlerCacheable
{
    private readonly IVesselLocationSnapshotRepository _locationRepository;

    public GetCurrentVesselLocationQueryHandler(IVesselLocationSnapshotRepository locationRepository)
    {
        _locationRepository = locationRepository;
    }

    public override async Task<VesselLocationSnapshotDto?> Handle(GetCurrentVesselLocationQuery request, CancellationToken cancellationToken)
    {
        var snapshot = await _locationRepository.GetCurrentAsync(request.VesselId, cancellationToken);
        return snapshot?.ToDto();
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };
}
