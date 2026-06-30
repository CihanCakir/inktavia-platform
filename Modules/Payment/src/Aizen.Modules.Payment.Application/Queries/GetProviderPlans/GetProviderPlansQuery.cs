using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.Payment.Application.Queries.GetProviderPlans;

public sealed class GetProviderPlansQuery : AizenQuery<List<ProviderPlanDto>> { }

public sealed record ProviderPlanDto(
    long    Id,
    string  PlanCode,
    string  Name,
    decimal MonthlyPriceTRY,
    int?    MaxActiveOffers,
    bool    HasPriorityBoost,
    bool    HasFullAnalytics,
    bool    IsFree,
    int     SortOrder
);
