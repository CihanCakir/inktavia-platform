using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;
using Aizen.Modules.Vessel.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Vessel.Application.Query.Vessel;

[DocumentationInfo("Get Vessel Names By Ids Query Handler", "Single query: SELECT Id, Name FROM Vessels WHERE Id IN (...).")]
public sealed class GetVesselNamesByIdsQueryHandler
    : AizenQueryHandler<GetVesselNamesByIdsQuery, List<VesselNameDto>>
{
    private readonly VesselDbContext _db;

    public GetVesselNamesByIdsQueryHandler(VesselDbContext db)
    {
        _db = db;
    }

    public override async Task<List<VesselNameDto>> Handle(
        GetVesselNamesByIdsQuery request, CancellationToken cancellationToken)
    {
        if (request.VesselIds.Length == 0)
            return new List<VesselNameDto>();

        return await _db.Vessels
            .Where(v => request.VesselIds.Contains(v.Id))
            .Select(v => new VesselNameDto { VesselId = v.Id, Name = v.Name })
            .ToListAsync(cancellationToken);
    }
}
