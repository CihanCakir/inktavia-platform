using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Domain.Entities.Plan;

namespace Aizen.Modules.Payment.Application.Queries.GetParticipantPlans;

public sealed class GetParticipantPlansQuery : AizenQuery<List<ParticipantPlanDto>> { }

public sealed record ParticipantPlanDto(
    long    Id,
    string  PlanCode,
    string  Name,
    string? Description,
    decimal MonthlyPriceTRY,
    decimal? AnnualPriceTRY,
    int?    TrialDays,
    string? BadgeLabel,
    decimal ServiceDiscountRate,
    decimal CargoDryDiscountRate,
    decimal InkCoinEarnMultiplier,
    bool    IsFree,
    bool    IsActive,
    int     SortOrder,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    List<PlanFeatureItem> FeatureItems
);
