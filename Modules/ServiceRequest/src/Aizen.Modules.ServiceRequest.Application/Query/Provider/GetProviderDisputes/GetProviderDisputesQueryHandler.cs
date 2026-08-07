using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Provider;
using Aizen.Modules.ServiceRequest.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ServiceRequest.Application.Query.Provider.GetProviderDisputes;

/// <summary>
/// A provider is a party to a dispute on every service request they <b>won</b> — the same accepted-offer link
/// <c>GetDisputeCaseDetail</c> uses to derive the dispute's provider. So "my disputes" = disputes on SRs where this
/// provider holds the accepted offer. One grouped query — no N+1. Cost-free: only the dispute + SR header cross,
/// never economics. Follows the <c>GetProviderConversations</c> DbContext-projection idiom for provider composed reads.
/// </summary>
public sealed class GetProviderDisputesQueryHandler
    : AizenQueryHandler<GetProviderDisputesQuery, GetProviderDisputesResponse>
{
    private readonly ServiceRequestDbContext _db;
    private readonly IAizenInfoAccessor _info;

    public GetProviderDisputesQueryHandler(ServiceRequestDbContext db, IAizenInfoAccessor info)
    {
        _db = db;
        _info = info;
    }

    public override async Task<GetProviderDisputesResponse?> Handle(
        GetProviderDisputesQuery request, CancellationToken ct)
    {
        var profileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
        if (profileId <= 0)
            // no provider link → no disputes (never fabricate)
            return new GetProviderDisputesResponse { PageIndex = request.PageIndex, PageSize = request.PageSize };

        var userId = _info.UserInfoAccessor.UserInfo.UserId;

        // SR ids this provider won (accepted offer) — the party link. A dispute only exists post-acceptance, so the
        // accepted-offer provider is exactly the provider party to it.
        var mySrIds = _db.ServiceRequestOffers
            .AsNoTracking()
            .Where(o => o.ProviderProfileId == profileId
                        && o.Status == ServiceRequestOfferStatus.Accepted
                        && !o.IsDeleted)
            .Select(o => o.ServiceRequestId);

        var mine = _db.ServiceRequestDisputes
            .AsNoTracking()
            .Where(d => !d.IsDeleted && mySrIds.Contains(d.ServiceRequestId));

        // Global open/actionable count (independent of the page's status filter) — the dashboard row reads this.
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

        var items = page.Select(x => new ProviderDisputeItemDto
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

        return new GetProviderDisputesResponse
        {
            PageIndex = request.PageIndex,
            PageSize = request.PageSize,
            Total = total,
            OpenCount = openCount,
            Items = items
        };
    }
}
