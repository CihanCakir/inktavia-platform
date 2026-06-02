using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Interface.Service;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Modules.Vessel.Application.Query.Vessel;

[DocumentationInfo("Get Vessel Detail Query Handler", "Builds and returns a full vessel detail snapshot; cached for 15 minutes.")]
public sealed class GetVesselDetailQueryHandler : AizenQueryHandler<GetVesselDetailQuery, GetVesselDetailResponse>, IAizenQueryHandlerCacheable
{
    private readonly IVesselSnapshotService _snapshotService;

    public GetVesselDetailQueryHandler(IVesselSnapshotService snapshotService)
    {
        _snapshotService = snapshotService;
    }

    public override async Task<GetVesselDetailResponse?> Handle(GetVesselDetailQuery request, CancellationToken cancellationToken)
    {
        var vessel = await _snapshotService.BuildDetailAsync(request.VesselId, cancellationToken);
        return vessel is null ? null : new GetVesselDetailResponse(vessel);
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) };
}
