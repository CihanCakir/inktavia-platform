using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Application.Queries.GetProviderPlans;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetProviderPlanById;

[DocumentationInfo("GetProviderPlanByIdQueryHandler",
    "Returns a single active provider plan by its primary key. Returns null when not found.")]
public sealed class GetProviderPlanByIdQueryHandler
    : AizenQueryHandler<GetProviderPlanByIdQuery, ProviderPlanDto>
{
    private readonly IProviderPlanRepository _plans;

    public GetProviderPlanByIdQueryHandler(IProviderPlanRepository plans)
        => _plans = plans;

    public override async Task<ProviderPlanDto?> Handle(
        GetProviderPlanByIdQuery request, CancellationToken ct)
    {
        var p = await _plans.GetByIdAsync(request.Id, ct);
        if (p is null) return null;

        return new ProviderPlanDto(
            p.Id, p.PlanCode, p.Name, p.Description,
            p.MonthlyPriceTRY, p.AnnualPriceTRY, p.TrialDays, p.BadgeLabel,
            p.MaxActiveOffers, p.HasPriorityBoost, p.HasFullAnalytics,
            p.IsFree, p.IsActive, p.SortOrder, p.ValidFrom, p.ValidTo, p.FeatureItems);
    }
}
