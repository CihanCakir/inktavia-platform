using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Application.Mapping;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Abstraction.Response.Specification;

namespace Aizen.Modules.Vessel.Application.Query.Specification;

[DocumentationInfo("Get Vessel Specification Query Handler", "Returns the specification for a vessel; cached for 15 minutes.")]
public sealed class GetVesselSpecificationQueryHandler : AizenQueryHandler<GetVesselSpecificationQuery, GetVesselSpecificationResponse>, IAizenQueryHandlerCacheable
{
    private readonly IVesselSpecificationRepository _specRepository;

    public GetVesselSpecificationQueryHandler(IVesselSpecificationRepository specRepository)
    {
        _specRepository = specRepository;
    }

    public override async Task<GetVesselSpecificationResponse?> Handle(GetVesselSpecificationQuery request, CancellationToken cancellationToken)
    {
        var spec = await _specRepository.GetByVesselIdAsync(request.VesselId, cancellationToken);
        return spec is null ? null : new GetVesselSpecificationResponse(spec.ToDto());
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) };
}
