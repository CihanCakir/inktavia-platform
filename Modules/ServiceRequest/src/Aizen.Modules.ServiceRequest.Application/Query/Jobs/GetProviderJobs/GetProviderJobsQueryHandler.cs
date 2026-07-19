using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Jobs;
using Aizen.Modules.ServiceRequest.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ServiceRequest.Application.Query.Jobs;

/// <summary>
/// Returns the caller provider's own assignments with in-module SR enrichment (Title, RequestCode, VesselId).
/// Provider profile from the assertion. One query with join — no N+1.
/// </summary>
[DocumentationInfo("Get provider jobs query handler", "Lists provider assignments scoped by the asserted provider profile id, enriched with SR title/code.")]
public sealed class GetProviderJobsQueryHandler : AizenQueryHandler<GetProviderJobsQuery, GetProviderJobsResponse>
{
    private readonly ServiceRequestDbContext _db;
    private readonly IAizenInfoAccessor _info;

    public GetProviderJobsQueryHandler(ServiceRequestDbContext db, IAizenInfoAccessor info)
    {
        _db = db;
        _info = info;
    }

    public override async Task<GetProviderJobsResponse?> Handle(GetProviderJobsQuery request, CancellationToken cancellationToken)
    {
        var providerProfileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;

        if (providerProfileId <= 0)
            return new GetProviderJobsResponse { PageIndex = request.PageIndex, PageSize = request.PageSize };

        var skip = request.PageIndex * request.PageSize;

        var items = await _db.ServiceRequestAssignments
            .AsNoTracking()
            .Where(a => a.ProviderProfileId == providerProfileId && !a.IsDeleted)
            .Join(_db.ServiceRequests, a => a.ServiceRequestId, sr => sr.Id, (a, sr) => new { a, sr })
            .OrderByDescending(x => x.a.CreateDate)
            .Skip(skip)
            .Take(request.PageSize)
            .Select(x => new ProviderJobItemDto
            {
                AssignmentId = x.a.Id,
                ServiceRequestId = x.a.ServiceRequestId,
                ServiceRequestOfferId = x.a.ServiceRequestOfferId,
                Status = x.sr.Status.ToString(),
                Title = x.sr.Title,
                RequestCode = x.sr.RequestCode,
                VesselId = x.sr.VesselId,
                VesselName = x.sr.VesselName,
                ScheduledStartDate = x.a.ScheduledStartDate,
                ScheduledEndDate = x.a.ScheduledEndDate,
                ActualStartDate = x.a.ActualStartDate,
                ActualEndDate = x.a.ActualEndDate,
                ProviderNotes = x.a.ProviderNotes
            })
            .ToListAsync(cancellationToken);

        return new GetProviderJobsResponse
        {
            Items = items,
            PageIndex = request.PageIndex,
            PageSize = request.PageSize
        };
    }
}
