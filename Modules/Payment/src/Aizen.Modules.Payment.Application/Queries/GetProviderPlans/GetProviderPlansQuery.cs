using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Domain.Entities.Plan;

namespace Aizen.Modules.Payment.Application.Queries.GetProviderPlans;

public sealed class GetProviderPlansQuery : AizenQuery<List<ProviderPlanDto>> { }

public sealed record ProviderPlanDto(
    long                   Id,
    string                 PlanCode,
    string                 Name,
    string?                Description,
    decimal                MonthlyPriceTRY,
    decimal?               AnnualPriceTRY,
    int?                   TrialDays,
    string?                BadgeLabel,
    int?                   MaxActiveOffers,
    bool                   HasPriorityBoost,
    bool                   HasFullAnalytics,
    bool                   IsFree,
    int                    SortOrder,
    DateTime?              ValidFrom,
    DateTime?              ValidTo,
    List<PlanFeatureItem>  FeatureItems
);
