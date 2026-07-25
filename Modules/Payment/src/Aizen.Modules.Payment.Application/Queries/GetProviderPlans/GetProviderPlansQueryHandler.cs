using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Abstraction.Dto;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetProviderPlans;

public sealed class GetProviderPlansQueryHandler
    : AizenQueryHandler<GetProviderPlansQuery, List<ProviderPlanDto>>
{
    private readonly IProviderPlanRepository _repo;

    public GetProviderPlansQueryHandler(IProviderPlanRepository repo) => _repo = repo;

    public override async Task<List<ProviderPlanDto>?> Handle(
        GetProviderPlansQuery request, CancellationToken ct)
    {
        var plans = await _repo.GetAllActiveAsync(ct);
        var activeSub = await _repo.GetActiveSubscriptionAsync(request.ProviderProfileId, DateTime.UtcNow, ct);

        return plans
            .OrderBy(p => p.SortOrder)
            .Select(p => new ProviderPlanDto
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
            })
            .ToList();
    }
}
