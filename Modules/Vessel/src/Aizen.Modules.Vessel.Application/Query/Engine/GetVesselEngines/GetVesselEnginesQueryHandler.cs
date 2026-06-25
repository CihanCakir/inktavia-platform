using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Vessel.Abstraction.Dto.Engine;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Aizen.Modules.Vessel.Repository.Persistence;
using Aizen.Modules.Vessel.Abstraction.Response.Engine;
using MiniUow.Paging;

namespace Aizen.Modules.Vessel.Application.Query.Engine;

[DocumentationInfo("Get Vessel Engines Query Handler", "Returns a paged list of engines for a vessel; cached for 15 minutes.")]
public sealed class GetVesselEnginesQueryHandler : AizenQueryHandler<GetVesselEnginesQuery, GetVesselEnginesResponse>, IAizenQueryHandlerCacheable
{
    private readonly IAizenUnitOfWork<VesselDbContext> _uow;

    public GetVesselEnginesQueryHandler(IAizenUnitOfWork<VesselDbContext> uow)
    {
        _uow = uow;
    }

    public override async Task<GetVesselEnginesResponse?> Handle(GetVesselEnginesQuery request, CancellationToken cancellationToken)
    {
        var repo = _uow.GetRepository<VesselEngineEntity>();

        var result = await repo.GetPagedListAsync<VesselEngineDto>(
            selector: e => new VesselEngineDto
            {
                Id = e.Id,
                VesselId = e.VesselId,
                EngineName = e.EngineName,
                EngineTypeCode = e.EngineTypeCode,
                FuelTypeCode = e.FuelTypeCode,
                Brand = e.Brand,
                Model = e.Model,
                SerialNumber = e.SerialNumber,
                HorsePower = e.HorsePower,
                ProductionYear = e.ProductionYear,
                IsPrimary = e.IsPrimary,
                IsActive = e.IsActive
            },
            predicate: e => e.VesselId == request.VesselId,
            orderBy: q => q.OrderByDescending(e => e.IsPrimary),
            pageIndex: request.PageIndex,
            pageSize: request.PageSize,
            cancellationToken: cancellationToken);

        return new GetVesselEnginesResponse((Paginate<VesselEngineDto>)result);
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) };
}
