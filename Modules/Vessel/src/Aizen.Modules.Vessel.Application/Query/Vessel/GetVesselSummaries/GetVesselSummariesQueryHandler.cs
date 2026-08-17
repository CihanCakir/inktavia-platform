using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;
using Aizen.Modules.Vessel.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Vessel.Application.Query.Vessel.GetVesselSummaries;

[DocumentationInfo("Get Vessel Summaries Query Handler", "Single query: fetches vessel + specification for a batch of ids and projects into lightweight summary DTOs.")]
public sealed class GetVesselSummariesQueryHandler
    : AizenQueryHandler<GetVesselSummariesQuery, GetVesselSummariesResponse>
{
    private readonly VesselDbContext _db;

    public GetVesselSummariesQueryHandler(VesselDbContext db)
    {
        _db = db;
    }

    public override async Task<GetVesselSummariesResponse?> Handle(
        GetVesselSummariesQuery request, CancellationToken cancellationToken)
    {
        if (request.Ids.Length == 0)
            return new GetVesselSummariesResponse();

        if (request.Ids.Length > 100)
            throw new ArgumentException("Cannot request more than 100 vessel summaries at once.");

        var items = await _db.Vessels
            .AsNoTracking()
            .Where(v => request.Ids.Contains(v.Id) && !v.IsDeleted)
            .GroupJoin(
                _db.VesselSpecifications.AsNoTracking(),
                v => v.Id,
                s => s.VesselId,
                (v, specs) => new { Vessel = v, Specs = specs })
            .SelectMany(
                x => x.Specs.DefaultIfEmpty(),
                (x, spec) => new VesselSummaryDto
                {
                    VesselId = x.Vessel.Id,
                    Name = x.Vessel.Name,
                    VesselTypeCode = x.Vessel.VesselTypeCode,
                    Brand = spec != null ? spec.Brand : null,
                    Model = spec != null ? spec.Model : null,
                    LengthValue = spec != null ? spec.LengthValue : null,
                    LengthUnitCode = spec != null ? spec.LengthUnitCode : null,
                    ProductionYear = spec != null ? spec.ProductionYear : null,
                    HullMaterialCode = spec != null ? spec.HullMaterialCode : null,
                    RegistrationNumber = x.Vessel.RegistrationNumber,
                    BeamValue = spec != null ? spec.BeamValue : null,
                    BeamUnitCode = spec != null ? spec.BeamUnitCode : null,
                    DraftValue = spec != null ? spec.DraftValue : null,
                    DraftUnitCode = spec != null ? spec.DraftUnitCode : null
                })
            .ToListAsync(cancellationToken);

        return new GetVesselSummariesResponse { Items = items };
    }
}
