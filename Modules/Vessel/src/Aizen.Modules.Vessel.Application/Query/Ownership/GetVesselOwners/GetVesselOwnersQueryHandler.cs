using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Dto.Ownership;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Application.Mapping;
using Aizen.Modules.Vessel.Domain.Interface.Repository;

namespace Aizen.Modules.Vessel.Application.Query.Ownership;

[DocumentationInfo("Get Vessel Owners Query Handler", "Returns all ownership records for a vessel; cached for 15 minutes.")]
public sealed class GetVesselOwnersQueryHandler : AizenQueryHandler<GetVesselOwnersQuery, IReadOnlyList<VesselOwnerDto>>, IAizenQueryHandlerCacheable
{
    private readonly IVesselOwnerRepository _ownerRepository;

    public GetVesselOwnersQueryHandler(IVesselOwnerRepository ownerRepository)
    {
        _ownerRepository = ownerRepository;
    }

    public override async Task<IReadOnlyList<VesselOwnerDto>> Handle(GetVesselOwnersQuery request, CancellationToken cancellationToken)
    {
        var owners = await _ownerRepository.GetByVesselIdAsync(request.VesselId, cancellationToken);
        return owners.Select(o => o.ToDto()).ToList().AsReadOnly();
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) };
}
