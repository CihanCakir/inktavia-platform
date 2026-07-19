using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Jobs;
using Aizen.Modules.ServiceRequest.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ServiceRequest.Application.Query.Jobs;

/// <summary>
/// Jobs needing intervention, grouped into ownerApproval / materialRequired / blocked.
/// In-module join with SR for title/code/vesselId. Capped at 5 per group.
/// </summary>
public sealed class GetProviderJobsActionRequiredQueryHandler
    : AizenQueryHandler<GetProviderJobsActionRequiredQuery, GetProviderJobsActionRequiredResponse>
{
    private static readonly ServiceRequestStatus[] OwnerApprovalStatuses =
        { ServiceRequestStatus.WaitingForOwnerApproval, ServiceRequestStatus.CompletionSubmitted };
    private static readonly ServiceRequestStatus[] MaterialStatuses =
        { ServiceRequestStatus.WaitingForMaterial };
    private static readonly ServiceRequestStatus[] BlockedStatuses =
        { ServiceRequestStatus.Paused };

    private readonly ServiceRequestDbContext _db;
    private readonly IAizenInfoAccessor _info;

    public GetProviderJobsActionRequiredQueryHandler(ServiceRequestDbContext db, IAizenInfoAccessor info)
    { _db = db; _info = info; }

    public override async Task<GetProviderJobsActionRequiredResponse?> Handle(
        GetProviderJobsActionRequiredQuery request, CancellationToken ct)
    {
        var profileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
        if (profileId <= 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        var allStatuses = OwnerApprovalStatuses.Concat(MaterialStatuses).Concat(BlockedStatuses).ToArray();

        var items = await _db.ServiceRequestAssignments
            .AsNoTracking()
            .Where(a => a.ProviderProfileId == profileId && !a.IsDeleted)
            .Join(_db.ServiceRequests, a => a.ServiceRequestId, sr => sr.Id, (a, sr) => new { a, sr })
            .Where(x => allStatuses.Contains(x.sr.Status))
            .Select(x => new ActionRequiredJobDto
            {
                AssignmentId = x.a.Id,
                ServiceRequestId = x.a.ServiceRequestId,
                Status = x.sr.Status.ToString(),
                Title = x.sr.Title,
                RequestCode = x.sr.RequestCode,
                VesselId = x.sr.VesselId,
                VesselName = x.sr.VesselName,
                ScheduledStartDate = x.a.ScheduledStartDate
            })
            .ToListAsync(ct);

        return new GetProviderJobsActionRequiredResponse
        {
            OwnerApproval = items.Where(i => OwnerApprovalStatuses.Select(s => s.ToString()).Contains(i.Status))
                .OrderBy(i => i.ScheduledStartDate).Take(5).ToList(),
            MaterialRequired = items.Where(i => MaterialStatuses.Select(s => s.ToString()).Contains(i.Status))
                .OrderBy(i => i.ScheduledStartDate).Take(5).ToList(),
            Blocked = items.Where(i => BlockedStatuses.Select(s => s.ToString()).Contains(i.Status))
                .OrderBy(i => i.ScheduledStartDate).Take(5).ToList()
        };
    }
}
