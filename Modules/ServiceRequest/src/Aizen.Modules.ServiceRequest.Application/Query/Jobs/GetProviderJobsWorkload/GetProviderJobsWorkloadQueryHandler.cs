using System.Globalization;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Jobs;
using Aizen.Modules.ServiceRequest.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ServiceRequest.Application.Query.Jobs;

/// <summary>
/// Weekly workload: per-week buckets with scheduled/inProgress/completed counts.
/// One query, bucketed in memory. Monday-start ISO weeks, UTC.
/// </summary>
public sealed class GetProviderJobsWorkloadQueryHandler
    : AizenQueryHandler<GetProviderJobsWorkloadQuery, GetProviderJobsWorkloadResponse>
{
    private readonly ServiceRequestDbContext _db;
    private readonly IAizenInfoAccessor _info;

    public GetProviderJobsWorkloadQueryHandler(ServiceRequestDbContext db, IAizenInfoAccessor info)
    { _db = db; _info = info; }

    public override async Task<GetProviderJobsWorkloadResponse?> Handle(
        GetProviderJobsWorkloadQuery request, CancellationToken ct)
    {
        var profileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
        if (profileId <= 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        var weeks = Math.Clamp(request.Weeks, 1, 12);
        // UtcNow.Date is Kind=Unspecified — force Utc so windowEnd can be used in the timestamptz WHERE below.
        var today = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        var weekStart = today.AddDays(-(((int)today.DayOfWeek + 6) % 7)); // Monday
        var windowEnd = weekStart.AddDays(weeks * 7);

        // Fetch all assignments for this provider with relevant dates in the window
        var jobs = await _db.ServiceRequestAssignments
            .AsNoTracking()
            .Where(a => a.ProviderProfileId == profileId && !a.IsDeleted)
            .Join(_db.ServiceRequests, a => a.ServiceRequestId, sr => sr.Id, (a, sr) => new
            {
                a.ScheduledStartDate,
                a.ActualStartDate,
                a.ActualEndDate,
                SrStatus = sr.Status
            })
            .Where(x => x.ScheduledStartDate < windowEnd || x.ActualEndDate < windowEnd)
            .ToListAsync(ct);

        var buckets = new List<WorkloadWeekBucket>();
        for (int i = 0; i < weeks; i++)
        {
            var ws = weekStart.AddDays(i * 7);
            var we = ws.AddDays(7);

            var scheduled = jobs.Count(j =>
                j.ScheduledStartDate >= ws && j.ScheduledStartDate < we
                && (j.SrStatus == ServiceRequestStatus.Assigned || j.SrStatus == ServiceRequestStatus.Scheduled));

            var inProgress = jobs.Count(j =>
                (j.ActualStartDate ?? j.ScheduledStartDate) >= ws
                && (j.ActualStartDate ?? j.ScheduledStartDate) < we
                && j.SrStatus == ServiceRequestStatus.InProgress);

            var completed = jobs.Count(j =>
                j.ActualEndDate >= ws && j.ActualEndDate < we
                && j.SrStatus == ServiceRequestStatus.Completed);

            var weekNum = ISOWeek.GetWeekOfYear(ws);
            buckets.Add(new WorkloadWeekBucket
            {
                WeekStartUtc = ws,
                Label = $"H{weekNum}",
                Scheduled = scheduled,
                InProgress = inProgress,
                Completed = completed
            });
        }

        return new GetProviderJobsWorkloadResponse { Weeks = buckets };
    }
}
