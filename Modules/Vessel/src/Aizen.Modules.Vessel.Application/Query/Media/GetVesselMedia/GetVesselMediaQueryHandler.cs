using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Vessel.Abstraction.Dto.Media;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Aizen.Modules.Vessel.Repository.Persistence;
using Aizen.Modules.Vessel.Abstraction.Response.Media;

namespace Aizen.Modules.Vessel.Application.Query.Media;

[DocumentationInfo("Get Vessel Media Query Handler", "Returns a paged list of media items for a vessel ordered by sort order; cached for 15 minutes.")]
public sealed class GetVesselMediaQueryHandler : AizenQueryHandler<GetVesselMediaQuery, GetVesselMediaResponse>, IAizenQueryHandlerCacheable
{
    private readonly IAizenUnitOfWork<VesselDbContext> _uow;

    public GetVesselMediaQueryHandler(IAizenUnitOfWork<VesselDbContext> uow)
    {
        _uow = uow;
    }

    public override async Task<GetVesselMediaResponse?> Handle(GetVesselMediaQuery request, CancellationToken cancellationToken)
    {
        var repo = _uow.GetRepository<VesselMediaEntity>();

        var result = await repo.GetPagedListAsync<VesselMediaDto>(
            selector: m => new VesselMediaDto
            {
                Id = m.Id,
                VesselId = m.VesselId,
                MediaType = m.MediaType,
                FileId = m.FileId,
                FileName = m.FileName,
                FileUrl = m.FileUrl,
                SortOrder = m.SortOrder,
                IsCover = m.IsCover,
                IsActive = m.IsActive
            },
            predicate: m => m.VesselId == request.VesselId,
            orderBy: q => q.OrderBy(m => m.SortOrder),
            pageIndex: request.PageIndex,
            pageSize: request.PageSize,
            cancellationToken: cancellationToken);

        return new GetVesselMediaResponse(result);
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) };
}
