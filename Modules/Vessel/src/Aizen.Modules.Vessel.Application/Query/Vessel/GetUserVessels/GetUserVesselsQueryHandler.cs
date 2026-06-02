using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;
using Aizen.Modules.Vessel.Abstraction.Enum;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Aizen.Modules.Vessel.Repository.Persistence;
using MiniUow.Paging;

namespace Aizen.Modules.Vessel.Application.Query.Vessel;

[DocumentationInfo("Get User Vessels Query Handler", "Returns a paged list of vessels owned by the user; cached for 10 minutes.")]
public sealed class GetUserVesselsQueryHandler : AizenQueryHandler<GetUserVesselsQuery, IPaginate<VesselListItemDto>>, IAizenQueryHandlerCacheable
{
    private readonly IAizenUnitOfWork<VesselDbContext> _uow;

    public GetUserVesselsQueryHandler(IAizenUnitOfWork<VesselDbContext> uow)
    {
        _uow = uow;
    }

    public override async Task<IPaginate<VesselListItemDto>> Handle(GetUserVesselsQuery request, CancellationToken cancellationToken)
    {
        var repo = _uow.GetRepository<VesselOwnerEntity>();

        return await repo.GetPagedListAsync<VesselListItemDto>(
            selector: o => new VesselListItemDto
            {
                Id = o.Vessel!.Id,
                PublicId = o.Vessel.PublicId,
                VesselCode = o.Vessel.VesselCode,
                Name = o.Vessel.Name,
                Slug = o.Vessel.Slug,
                VesselTypeCode = o.Vessel.VesselTypeCode,
                FlagCountryCode = o.Vessel.FlagCountryCode,
                CoverMediaUrl = null,
                Status = o.Vessel.Status,
                Visibility = o.Vessel.Visibility,
                IsArchived = o.Vessel.IsArchived,
                CreateDate = o.Vessel.CreateDate
            },
            predicate: o => o.UserId == request.UserId && o.OwnershipStatus == VesselOwnershipStatus.Active,
            orderBy: q => q.OrderByDescending(o => o.Vessel!.CreateDate),
            pageIndex: request.PageIndex,
            pageSize: request.PageSize,
            cancellationToken: cancellationToken);
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) };
}
