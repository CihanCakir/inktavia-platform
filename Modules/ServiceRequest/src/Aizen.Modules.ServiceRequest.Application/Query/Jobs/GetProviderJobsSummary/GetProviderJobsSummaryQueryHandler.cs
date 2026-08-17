using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Jobs;
using Aizen.Modules.ServiceRequest.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ServiceRequest.Application.Query.Jobs;

public sealed class GetProviderJobsSummaryQueryHandler
    : AizenQueryHandler<GetProviderJobsSummaryQuery, GetProviderJobsSummaryResponse>
{
    private readonly ServiceRequestDbContext _db;
    private readonly IAizenInfoAccessor _info;

    public GetProviderJobsSummaryQueryHandler(ServiceRequestDbContext db, IAizenInfoAccessor info)
    { _db = db; _info = info; }

    public override async Task<GetProviderJobsSummaryResponse?> Handle(
        GetProviderJobsSummaryQuery request, CancellationToken ct)
    {
        var profileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
        if (profileId <= 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        // Group by ServiceRequest status (the design's lifecycle is tracked at the SR level)
        var counts = await _db.ServiceRequestAssignments
            .AsNoTracking()
            .Where(a => a.ProviderProfileId == profileId && !a.IsDeleted)
            .Join(_db.ServiceRequests, a => a.ServiceRequestId, sr => sr.Id, (a, sr) => sr.Status)
            .GroupBy(status => status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        int Get(ServiceRequestStatus s) => counts.FirstOrDefault(c => c.Status == s)?.Count ?? 0;

        var assigned = Get(ServiceRequestStatus.Assigned);
        var scheduled = Get(ServiceRequestStatus.Scheduled);
        var inProgress = Get(ServiceRequestStatus.InProgress);
        var waitingApproval = Get(ServiceRequestStatus.WaitingForOwnerApproval);
        var waitingMaterial = Get(ServiceRequestStatus.WaitingForMaterial);
        var paused = Get(ServiceRequestStatus.Paused);
        var completionSubmitted = Get(ServiceRequestStatus.CompletionSubmitted);
        var completed = Get(ServiceRequestStatus.Completed);

        var total = counts.Sum(c => c.Count);
        var active = total - completed;

        return new GetProviderJobsSummaryResponse
        {
            Assigned = assigned, Scheduled = scheduled, InProgress = inProgress,
            WaitingForOwnerApproval = waitingApproval, WaitingForMaterial = waitingMaterial,
            Paused = paused, CompletionSubmitted = completionSubmitted, Completed = completed,
            Active = active, Total = total
        };
    }
}
