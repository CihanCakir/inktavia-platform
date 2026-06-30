using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Application.Mapping;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Modules.Vessel.Application.Query.Vessel;

[DocumentationInfo("Get Vessel By Id Query Handler", "Returns a vessel DTO by its primary key; cached for 15 minutes.")]
public sealed class GetVesselByIdQueryHandler : AizenQueryHandler<GetVesselByIdQuery, GetVesselByIdResponse>, IAizenQueryHandlerCacheable
{
    private readonly IVesselRepository _vesselRepository;

    public GetVesselByIdQueryHandler(IVesselRepository vesselRepository)
    {
        _vesselRepository = vesselRepository;
    }

    public override async Task<GetVesselByIdResponse?> Handle(GetVesselByIdQuery request, CancellationToken cancellationToken)
    {
        var vessel = await _vesselRepository.GetByIdAsync(request.VesselId, cancellationToken);
        return vessel is null ? null : new GetVesselByIdResponse(vessel.ToDto());
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) };
}
