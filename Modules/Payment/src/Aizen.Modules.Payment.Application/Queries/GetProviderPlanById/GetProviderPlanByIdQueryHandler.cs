using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Abstraction.Dto;
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

        return new ProviderPlanDto
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
            IsCurrent        = false,
            Features         = p.FeatureItems?.Select(f => f.Text).ToList() ?? [],
        };
    }
}
