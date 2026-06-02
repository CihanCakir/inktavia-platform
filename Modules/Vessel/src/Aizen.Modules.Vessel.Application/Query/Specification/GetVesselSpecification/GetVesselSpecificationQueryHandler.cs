using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Dto.Specification;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Application.Mapping;
using Aizen.Modules.Vessel.Domain.Interface.Repository;

namespace Aizen.Modules.Vessel.Application.Query.Specification;

[DocumentationInfo("Get Vessel Specification Query Handler", "Returns the specification for a vessel; cached for 15 minutes.")]
public sealed class GetVesselSpecificationQueryHandler : AizenQueryHandler<GetVesselSpecificationQuery, VesselSpecificationDto?>, IAizenQueryHandlerCacheable
{
    private readonly IVesselSpecificationRepository _specRepository;

    public GetVesselSpecificationQueryHandler(IVesselSpecificationRepository specRepository)
    {
        _specRepository = specRepository;
    }

    public override async Task<VesselSpecificationDto?> Handle(GetVesselSpecificationQuery request, CancellationToken cancellationToken)
    {
        var spec = await _specRepository.GetByVesselIdAsync(request.VesselId, cancellationToken);
        return spec?.ToDto();
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) };
}
