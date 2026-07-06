using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetProviderPlans;

public sealed class GetProviderPlansQueryHandler
    : AizenQueryHandler<GetProviderPlansQuery, List<ProviderPlanDto>>
{
    private readonly IProviderPlanRepository _plans;

    public GetProviderPlansQueryHandler(IProviderPlanRepository plans)
        => _plans = plans;

    public override async Task<List<ProviderPlanDto>?> Handle(
        GetProviderPlansQuery request, CancellationToken ct)
    {
        var plans = await _plans.GetAllActiveAsync(ct);
        return plans.Select(p => new ProviderPlanDto(
            p.Id, p.PlanCode, p.Name, p.Description,
            p.MonthlyPriceTRY, p.AnnualPriceTRY, p.TrialDays, p.BadgeLabel,
            p.MaxActiveOffers, p.HasPriorityBoost, p.HasFullAnalytics,
            p.IsFree, p.IsActive, p.SortOrder, p.ValidFrom, p.ValidTo, p.FeatureItems)).ToList();
    }
}
