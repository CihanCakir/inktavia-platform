using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;
using Aizen.Modules.Vessel.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Vessel.Application.Query.Vessel;

[DocumentationInfo("Get Vessel Status Counts Query Handler", "Single query: SELECT Status, COUNT(*) FROM Vessels WHERE NOT IsDeleted GROUP BY Status; maps the enum to the lowercase FE/BFF key.")]
public sealed class GetVesselStatusCountsQueryHandler
    : AizenQueryHandler<GetVesselStatusCountsQuery, List<VesselStatusCountDto>>
{
    private readonly VesselDbContext _db;

    public GetVesselStatusCountsQueryHandler(VesselDbContext db)
    {
        _db = db;
    }

    public override async Task<List<VesselStatusCountDto>> Handle(
        GetVesselStatusCountsQuery request, CancellationToken cancellationToken)
    {
        // GROUP BY the status enum column (not a timestamptz) → no Npgsql grouping concern. Materialise the
        // enum→count pairs, then map the enum to its lowercase key in memory (EF cannot translate ToString()).
        var raw = await _db.Vessels
            .Where(v => !v.IsDeleted)
            .GroupBy(v => v.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return raw
            .Select(r => new VesselStatusCountDto
            {
                Status = r.Status.ToString().ToLowerInvariant(),
                Count = r.Count,
            })
            .ToList();
    }
}
