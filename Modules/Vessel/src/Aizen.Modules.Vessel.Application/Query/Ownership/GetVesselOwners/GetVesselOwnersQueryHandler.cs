using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Vessel.Abstraction.Dto.Ownership;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Aizen.Modules.Vessel.Repository.Persistence;
using Aizen.Modules.Vessel.Abstraction.Response.Ownership;

namespace Aizen.Modules.Vessel.Application.Query.Ownership;

[DocumentationInfo("Get Vessel Owners Query Handler", "Returns a paged list of ownership records for a vessel; cached for 15 minutes.")]
public sealed class GetVesselOwnersQueryHandler : AizenQueryHandler<GetVesselOwnersQuery, GetVesselOwnersResponse>, IAizenQueryHandlerCacheable
{
    private readonly IAizenUnitOfWork<VesselDbContext> _uow;

    public GetVesselOwnersQueryHandler(IAizenUnitOfWork<VesselDbContext> uow)
    {
        _uow = uow;
    }

    public override async Task<GetVesselOwnersResponse?> Handle(GetVesselOwnersQuery request, CancellationToken cancellationToken)
    {
        var repo = _uow.GetRepository<VesselOwnerEntity>();

        var result = await repo.GetPagedListAsync<VesselOwnerDto>(
            selector: o => new VesselOwnerDto
            {
                Id = o.Id,
                VesselId = o.VesselId,
                UserId = o.UserId,
                UserProfileId = o.UserProfileId,
                Role = o.Role,
                Status = o.OwnershipStatus,
                IsPrimary = o.IsPrimary,
                InvitedAt = o.InvitedAt,
                AcceptedAt = o.AcceptedAt,
                RemovedAt = o.RemovedAt
            },
            predicate: o => o.VesselId == request.VesselId,
            orderBy: q => q.OrderByDescending(o => o.IsPrimary),
            pageIndex: request.PageIndex,
            pageSize: request.PageSize,
            cancellationToken: cancellationToken);

        return new GetVesselOwnersResponse(result);
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) };
}
