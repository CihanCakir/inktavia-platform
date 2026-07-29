using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Abstraction.Dto;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Queries.GetProviderSubscription;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetProviderPlans;

/// <summary>
/// Provider plans read-model. BE-P4: additionally resolves each plan's versioned <c>ProviderPlanPrice</c> active now
/// (launch-vs-list label + amount + effective window). Additive — existing fields unchanged; null when no versioned price.
/// </summary>
public sealed class GetProviderPlansQueryHandler
    : AizenQueryHandler<GetProviderPlansQuery, List<ProviderPlanDto>>
{
    private readonly IProviderPlanRepository      _repo;
    private readonly IProviderPlanPriceRepository _prices;

    public GetProviderPlansQueryHandler(IProviderPlanRepository repo, IProviderPlanPriceRepository prices)
    {
        _repo   = repo;
        _prices = prices;
    }

    public override async Task<List<ProviderPlanDto>?> Handle(
        GetProviderPlansQuery request, CancellationToken ct)
    {
        var now       = DateTime.UtcNow;
        var plans     = await _repo.GetAllActiveAsync(ct);
        var activeSub = await _repo.GetActiveSubscriptionAsync(request.ProviderProfileId, now, ct);

        var result = new List<ProviderPlanDto>(plans.Count);
        foreach (var p in plans.OrderBy(p => p.SortOrder))
        {
            // BE-P4: the versioned price active now for this plan (Monthly, TRY) — launch vs list.
            var activeNow = await _prices.ResolveAsync(p.Id, "TRY", BillingPeriod.Monthly, now, ct);

            result.Add(new ProviderPlanDto
            {
                Id               = p.Id,
                PlanCode         = p.PlanCode,
                Name             = p.Name,
                Description      = p.Description,
                MonthlyPriceTRY  = p.MonthlyPriceTRY,
                AnnualPriceTRY   = p.AnnualPriceTRY,
                BadgeLabel       = p.BadgeLabel,
                MaxActiveOffers  = p.MaxActiveOffers,
                HasPriorityBoost = p.HasPriorityBoost,
                HasFullAnalytics = p.HasFullAnalytics,
                SortOrder        = p.SortOrder,
                IsCurrent        = activeSub != null && p.Id == activeSub.ProviderPlanId,
                Features         = p.FeatureItems?.Select(f => f.Text).ToList() ?? [],
                ActivePrice      = GetProviderSubscriptionQueryHandler.MapActivePrice(activeNow),
            });
        }
        return result;
    }
}
