using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.Payment.Application.Queries.GetParticipantPlans;

public sealed class GetParticipantPlansQuery : AizenQuery<List<ParticipantPlanDto>> { }

public sealed record ParticipantPlanDto(
    long    Id,
    string  PlanCode,
    string  Name,
    decimal MonthlyPriceTRY,
    decimal ServiceDiscountRate,
    decimal CargoDryDiscountRate,
    decimal InkCoinEarnMultiplier,
    int     SortOrder
);
