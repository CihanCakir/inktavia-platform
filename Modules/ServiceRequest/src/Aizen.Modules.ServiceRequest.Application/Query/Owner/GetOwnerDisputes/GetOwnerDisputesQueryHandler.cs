using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Owner;
using Aizen.Modules.ServiceRequest.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ServiceRequest.Application.Query.Owner.GetOwnerDisputes;

/// <summary>
/// BE-MO5 — an owner is a party to every dispute on a service request they own. So "my disputes" = disputes on SRs
/// where <c>sr.OwnerUserId</c> equals the caller. One grouped query — no N+1. Cost-free: only the dispute + SR header
/// cross, never economics. Mirrors <see cref="Provider.GetProviderDisputes.GetProviderDisputesQueryHandler"/> — the
/// owner scope is simpler (no accepted-offer join; the owner link lives directly on the SR).
/// </summary>
public sealed class GetOwnerDisputesQueryHandler
    : AizenQueryHandler<GetOwnerDisputesQuery, GetOwnerDisputesResponse>
{
    private readonly ServiceRequestDbContext _db;
    private readonly IAizenInfoAccessor _info;

    public GetOwnerDisputesQueryHandler(ServiceRequestDbContext db, IAizenInfoAccessor info)
    {
        _db = db;
        _info = info;
    }

    public override async Task<GetOwnerDisputesResponse?> Handle(
        GetOwnerDisputesQuery request, CancellationToken ct)
    {
        var userId = _info.UserInfoAccessor.UserInfo.UserId;
        if (userId <= 0)
            // no owner identity → no disputes (never fabricate)
            return new GetOwnerDisputesResponse { PageIndex = request.PageIndex, PageSize = request.PageSize };

        // SR ids this caller owns — the party link. The owner is a party to every dispute on their own SR.
        var mySrIds = _db.ServiceRequests
            .AsNoTracking()
            .Where(sr => sr.OwnerUserId == userId && !sr.IsDeleted)
            .Select(sr => sr.Id);

        var mine = _db.ServiceRequestDisputes
            .AsNoTracking()
            .Where(d => !d.IsDeleted && mySrIds.Contains(d.ServiceRequestId));

        // Global open/actionable count (independent of the page's status filter).
        var openCount = await mine.CountAsync(
            d => d.Status != ServiceRequestDisputeStatus.Resolved
                 && d.Status != ServiceRequestDisputeStatus.Closed, ct);

        var filtered = request.StatusFilter.HasValue
            ? mine.Where(d => d.Status == request.StatusFilter.Value)
            : mine;

        var total = await filtered.CountAsync(ct);

        var skip = request.PageIndex * request.PageSize;
        var page = await filtered
            .OrderByDescending(d => d.OpenedAt)
            .Skip(skip)
            .Take(request.PageSize)
            .Join(_db.ServiceRequests.AsNoTracking(),
                d => d.ServiceRequestId,
                sr => sr.Id,
                (d, sr) => new
                {
                    d.Id,
                    d.ServiceRequestId,
                    sr.RequestCode,
                    sr.Title,
                    sr.ServiceCategoryCode,
                    d.Status,
                    d.Reason,
                    d.Description,
                    d.OpenedByUserId,
                    d.OpenedAt,
                    d.ResolvedAt
                })
            .ToListAsync(ct);

        var items = page.Select(x => new OwnerDisputeItemDto
        {
            DisputeId = x.Id,
            ServiceRequestId = x.ServiceRequestId,
            ServiceRequestCode = x.RequestCode,
            ServiceRequestTitle = x.Title,
            ServiceCategoryCode = x.ServiceCategoryCode,
            Status = x.Status.ToString(),
            Reason = x.Reason.ToString(),
            Description = x.Description,
            IsOpen = x.Status != ServiceRequestDisputeStatus.Resolved
                     && x.Status != ServiceRequestDisputeStatus.Closed,
            OpenedByMe = x.OpenedByUserId == userId,
            OpenedAt = x.OpenedAt,
            ResolvedAt = x.ResolvedAt
        }).ToList();

        return new GetOwnerDisputesResponse
        {
            PageIndex = request.PageIndex,
            PageSize = request.PageSize,
            Total = total,
            OpenCount = openCount,
            Items = items
        };
    }
}
