using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;
using Aizen.Modules.Vessel.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Vessel.Application.Query.Vessel;

[DocumentationInfo("Get Vessel Counts By Owner User Ids Query Handler", "Single query: SELECT UserId, COUNT(*) FROM VesselOwners WHERE UserId IN (...) AND IsActive GROUP BY UserId.")]
public sealed class GetVesselCountsByOwnerUserIdsQueryHandler
    : AizenQueryHandler<GetVesselCountsByOwnerUserIdsQuery, List<VesselCountByOwnerDto>>
{
    private readonly VesselDbContext _db;

    public GetVesselCountsByOwnerUserIdsQueryHandler(VesselDbContext db)
    {
        _db = db;
    }

    public override async Task<List<VesselCountByOwnerDto>> Handle(
        GetVesselCountsByOwnerUserIdsQuery request, CancellationToken cancellationToken)
    {
        if (request.UserIds.Length == 0)
            return new List<VesselCountByOwnerDto>();

        return await _db.VesselOwners
            .Where(o => request.UserIds.Contains(o.UserId) && o.IsActive)
            .GroupBy(o => o.UserId)
            .Select(g => new VesselCountByOwnerDto { UserId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
    }
}
