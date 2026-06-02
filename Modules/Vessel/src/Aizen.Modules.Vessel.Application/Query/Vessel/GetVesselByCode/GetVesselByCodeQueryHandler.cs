using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Application.Mapping;
using Aizen.Modules.Vessel.Domain.Interface.Repository;

namespace Aizen.Modules.Vessel.Application.Query.Vessel;

[DocumentationInfo("Get Vessel By Code Query Handler", "Returns a vessel DTO by its vessel code; cached for 15 minutes.")]
public sealed class GetVesselByCodeQueryHandler : AizenQueryHandler<GetVesselByCodeQuery, VesselDto?>, IAizenQueryHandlerCacheable
{
    private readonly IVesselRepository _vesselRepository;

    public GetVesselByCodeQueryHandler(IVesselRepository vesselRepository)
    {
        _vesselRepository = vesselRepository;
    }

    public override async Task<VesselDto?> Handle(GetVesselByCodeQuery request, CancellationToken cancellationToken)
    {
        var vessel = await _vesselRepository.GetByCodeAsync(request.VesselCode, cancellationToken);
        return vessel?.ToDto();
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) };
}
