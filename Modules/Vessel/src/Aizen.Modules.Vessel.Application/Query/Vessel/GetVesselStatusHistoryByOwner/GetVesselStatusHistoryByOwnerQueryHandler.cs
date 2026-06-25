using Aizen.Core.CQRS.Handler;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Vessel.Abstraction.Dto.Status;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Abstraction.Response.Status;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Aizen.Modules.Vessel.Repository.Persistence;

namespace Aizen.Modules.Vessel.Application.Query.Vessel;

[DocumentationInfo("Get vessel status history by owner query handler", "Returns all status change events for vessels owned by a user; joins via VesselOwnerEntity.")]
public sealed class GetVesselStatusHistoryByOwnerQueryHandler
    : AizenQueryHandler<GetVesselStatusHistoryByOwnerQuery, GetVesselStatusHistoryByOwnerResponse>
{
    private readonly IAizenUnitOfWork<VesselDbContext> _uow;

    public GetVesselStatusHistoryByOwnerQueryHandler(IAizenUnitOfWork<VesselDbContext> uow)
    {
        _uow = uow;
    }

    public override async Task<GetVesselStatusHistoryByOwnerResponse?> Handle(
        GetVesselStatusHistoryByOwnerQuery request, CancellationToken cancellationToken)
    {
        var repo = _uow.GetRepository<VesselStatusHistoryEntity>();

        var result = await repo.GetPagedListAsync<VesselStatusHistoryByOwnerItemDto>(
            selector: h => new VesselStatusHistoryByOwnerItemDto
            {
                Id = h.Id,
                VesselId = h.VesselId,
                VesselName = h.Vessel!.Name,
                FromStatus = h.FromStatus,
                ToStatus = h.ToStatus,
                Reason = h.Reason,
                ChangedByUserId = h.ChangedByUserId,
                ChangedAt = h.ChangedAt
            },
            predicate: h => h.Vessel!.Owners.Any(o => o.UserId == request.OwnerUserId && o.IsActive),
            orderBy: q => q.OrderByDescending(h => h.ChangedAt),
            pageIndex: 0,
            pageSize: request.PageSize,
            cancellationToken: cancellationToken);

        return new GetVesselStatusHistoryByOwnerResponse
        {
            Items = result.Items.ToList()
        };
    }
}
