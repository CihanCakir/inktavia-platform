using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Dto.Engine;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Application.Mapping;
using Aizen.Modules.Vessel.Domain.Interface.Repository;

namespace Aizen.Modules.Vessel.Application.Query.Engine;

[DocumentationInfo("Get Vessel Engines Query Handler", "Returns all engines for a vessel; cached for 15 minutes.")]
public sealed class GetVesselEnginesQueryHandler : AizenQueryHandler<GetVesselEnginesQuery, IReadOnlyList<VesselEngineDto>>, IAizenQueryHandlerCacheable
{
    private readonly IVesselEngineRepository _engineRepository;

    public GetVesselEnginesQueryHandler(IVesselEngineRepository engineRepository)
    {
        _engineRepository = engineRepository;
    }

    public override async Task<IReadOnlyList<VesselEngineDto>> Handle(GetVesselEnginesQuery request, CancellationToken cancellationToken)
    {
        var engines = await _engineRepository.GetByVesselIdAsync(request.VesselId, cancellationToken);
        return engines.Select(e => e.ToDto()).ToList().AsReadOnly();
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) };
}
