using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Vessel.Abstraction.Dto.Status;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Aizen.Modules.Vessel.Repository.Persistence;
using Aizen.Modules.Vessel.Abstraction.Response.Status;

namespace Aizen.Modules.Vessel.Application.Query.Status;

[DocumentationInfo("Get Vessel Status History Query Handler", "Returns a paged status change history for a vessel; cached for 15 minutes.")]
public sealed class GetVesselStatusHistoryQueryHandler : AizenQueryHandler<GetVesselStatusHistoryQuery, GetVesselStatusHistoryResponse>, IAizenQueryHandlerCacheable
{
    private readonly IAizenUnitOfWork<VesselDbContext> _uow;

    public GetVesselStatusHistoryQueryHandler(IAizenUnitOfWork<VesselDbContext> uow)
    {
        _uow = uow;
    }

    public override async Task<GetVesselStatusHistoryResponse?> Handle(GetVesselStatusHistoryQuery request, CancellationToken cancellationToken)
    {
        var repo = _uow.GetRepository<VesselStatusHistoryEntity>();

        var result = await repo.GetPagedListAsync<VesselStatusHistoryDto>(
            selector: h => new VesselStatusHistoryDto
            {
                Id = h.Id,
                VesselId = h.VesselId,
                FromStatus = h.FromStatus,
                ToStatus = h.ToStatus,
                Reason = h.Reason,
                ChangedByUserId = h.ChangedByUserId,
                ChangedAt = h.ChangedAt
            },
            predicate: h => h.VesselId == request.VesselId,
            orderBy: q => q.OrderByDescending(h => h.ChangedAt),
            pageIndex: request.PageIndex,
            pageSize: request.PageSize,
            cancellationToken: cancellationToken);

        return new GetVesselStatusHistoryResponse(result);
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) };
}
