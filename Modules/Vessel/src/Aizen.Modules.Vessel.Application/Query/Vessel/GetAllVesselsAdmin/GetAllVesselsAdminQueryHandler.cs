using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Aizen.Modules.Vessel.Repository.Persistence;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Modules.Vessel.Application.Query.Vessel;

[DocumentationInfo("Get All Vessels Admin Query Handler", "Returns a paged list of all vessels for admin use; cached for 5 minutes.")]
public sealed class GetAllVesselsAdminQueryHandler : AizenQueryHandler<GetAllVesselsAdminQuery, GetAllVesselsAdminResponse>, IAizenQueryHandlerCacheable
{
    private readonly IAizenUnitOfWork<VesselDbContext> _uow;

    public GetAllVesselsAdminQueryHandler(IAizenUnitOfWork<VesselDbContext> uow)
    {
        _uow = uow;
    }

    public override async Task<GetAllVesselsAdminResponse?> Handle(GetAllVesselsAdminQuery request, CancellationToken cancellationToken)
    {
        var repo = _uow.GetRepository<VesselEntity>();

        var result = await repo.GetPagedListAsync<VesselListItemDto>(
            selector: v => new VesselListItemDto
            {
                Id = v.Id,
                PublicId = v.PublicId,
                VesselCode = v.VesselCode,
                Name = v.Name,
                Slug = v.Slug,
                VesselTypeCode = v.VesselTypeCode,
                FlagCountryCode = v.FlagCountryCode,
                CoverMediaUrl = null,
                Status = v.Status,
                Visibility = v.Visibility,
                IsArchived = v.IsArchived,
                CreateDate = v.CreateDate
            },
            predicate: v =>
                (request.IsArchived == null || v.IsArchived == request.IsArchived) &&
                (request.SearchTerm == null || v.Name.Contains(request.SearchTerm) || v.VesselCode.Contains(request.SearchTerm)),
            orderBy: q => q.OrderByDescending(v => v.CreateDate),
            pageIndex: request.PageIndex,
            pageSize: request.PageSize,
            cancellationToken: cancellationToken);

        return new GetAllVesselsAdminResponse(result);
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };
}
