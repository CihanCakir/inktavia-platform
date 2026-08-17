using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetAdminSubscriptionList;

[DocumentationInfo("GetAdminSubscriptionListQueryHandler",
    "Returns a paged, merged list of provider and participant subscriptions for admin oversight. " +
    "Loads plan names from both repositories and merges into unified AdminSubscriptionListItemDto rows. " +
    "Supports filtering by Audience (Provider/Participant) and Status.")]
public sealed class GetAdminSubscriptionListQueryHandler
    : AizenQueryHandler<GetAdminSubscriptionListQuery, AdminSubscriptionListResult>
{
    private readonly IProviderPlanRepository    _providerPlans;
    private readonly IParticipantPlanRepository _participantPlans;

    public GetAdminSubscriptionListQueryHandler(
        IProviderPlanRepository    providerPlans,
        IParticipantPlanRepository participantPlans)
    {
        _providerPlans    = providerPlans;
        _participantPlans = participantPlans;
    }

    public override async Task<AdminSubscriptionListResult?> Handle(
        GetAdminSubscriptionListQuery request, CancellationToken ct)
    {
        var audience = request.Audience?.Trim();
        var status   = request.Status?.Trim();

        var rows = new List<AdminSubscriptionListItemDto>();

        // ── Provider subscriptions ────────────────────────────────────────────
        if (string.IsNullOrEmpty(audience) || audience.Equals("Provider", StringComparison.OrdinalIgnoreCase))
        {
            var providerSubs = await _providerPlans.GetAllSubscriptionsForAdminAsync(status, ct);
            // load plan names as a dictionary to avoid N+1
            var planIds      = providerSubs.Select(x => x.ProviderPlanId).Distinct().ToList();
            var planLookup   = new Dictionary<long, (string Code, string Name)>();
            foreach (var id in planIds)
            {
                var plan = await _providerPlans.GetByIdAsync(id, ct);
                if (plan is not null) planLookup[id] = (plan.PlanCode, plan.Name);
            }

            foreach (var sub in providerSubs)
            {
                planLookup.TryGetValue(sub.ProviderPlanId, out var planInfo);
                rows.Add(new AdminSubscriptionListItemDto(
                    Id:         sub.Id,
                    Audience:   "Provider",
                    ProfileId:  sub.ProviderProfileId,
                    PlanId:     sub.ProviderPlanId,
                    PlanCode:   planInfo.Code  ?? "—",
                    PlanName:   planInfo.Name  ?? "—",
                    Status:     sub.Status.ToString(),
                    PaidAmount: sub.PaidAmount,
                    CurrencyCode: sub.CurrencyCode,
                    PeriodStart: sub.SubscriptionPeriodStart,
                    PeriodEnd:   sub.SubscriptionPeriodEnd,
                    AutoRenew:   sub.AutoRenew,
                    CreateDate:  sub.CreateDate ?? DateTime.UtcNow
                ));
            }
        }

        // ── Participant subscriptions ─────────────────────────────────────────
        if (string.IsNullOrEmpty(audience) || audience.Equals("Participant", StringComparison.OrdinalIgnoreCase))
        {
            var participantSubs = await _participantPlans.GetAllSubscriptionsForAdminAsync(status, ct);
            var planIds         = participantSubs.Select(x => x.ParticipantPlanId).Distinct().ToList();
            var planLookup      = new Dictionary<long, (string Code, string Name)>();
            foreach (var id in planIds)
            {
                var plan = await _participantPlans.GetByIdAsync(id, ct);
                if (plan is not null) planLookup[id] = (plan.PlanCode, plan.Name);
            }

            foreach (var sub in participantSubs)
            {
                planLookup.TryGetValue(sub.ParticipantPlanId, out var planInfo);
                rows.Add(new AdminSubscriptionListItemDto(
                    Id:         sub.Id,
                    Audience:   "Participant",
                    ProfileId:  sub.ParticipantProfileId,
                    PlanId:     sub.ParticipantPlanId,
                    PlanCode:   planInfo.Code ?? "—",
                    PlanName:   planInfo.Name ?? "—",
                    Status:     sub.Status.ToString(),
                    PaidAmount: sub.PaidAmount,
                    CurrencyCode: sub.CurrencyCode,
                    PeriodStart: sub.SubscriptionPeriodStart,
                    PeriodEnd:   sub.SubscriptionPeriodEnd,
                    AutoRenew:   sub.AutoRenew,
                    CreateDate:  sub.CreateDate ?? DateTime.UtcNow
                ));
            }
        }

        // sort merged list by CreateDate descending
        rows.Sort((a, b) => b.CreateDate.CompareTo(a.CreateDate));

        var total = rows.Count;
        var page  = Math.Max(1, request.Page);
        var size  = Math.Clamp(request.PageSize, 1, 100);

        var paged = rows
            .Skip((page - 1) * size)
            .Take(size)
            .ToList();

        return new AdminSubscriptionListResult(paged, total, page, size);
    }
}
