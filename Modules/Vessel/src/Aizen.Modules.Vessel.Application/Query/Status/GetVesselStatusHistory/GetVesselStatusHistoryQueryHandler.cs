using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Dto.Status;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Application.Mapping;
using Aizen.Modules.Vessel.Domain.Interface.Repository;

namespace Aizen.Modules.Vessel.Application.Query.Status;

[DocumentationInfo("Get Vessel Status History Query Handler", "Returns status change history for a vessel; cached for 15 minutes.")]
public sealed class GetVesselStatusHistoryQueryHandler : AizenQueryHandler<GetVesselStatusHistoryQuery, IReadOnlyList<VesselStatusHistoryDto>>, IAizenQueryHandlerCacheable
{
    private readonly IVesselStatusHistoryRepository _statusHistoryRepository;

    public GetVesselStatusHistoryQueryHandler(IVesselStatusHistoryRepository statusHistoryRepository)
    {
        _statusHistoryRepository = statusHistoryRepository;
    }

    public override async Task<IReadOnlyList<VesselStatusHistoryDto>> Handle(GetVesselStatusHistoryQuery request, CancellationToken cancellationToken)
    {
        var history = await _statusHistoryRepository.GetByVesselIdAsync(request.VesselId, cancellationToken);
        return history.Select(h => h.ToDto()).ToList().AsReadOnly();
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) };
}
