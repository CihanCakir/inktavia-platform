using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Application.Queries.GetParticipantPlans;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetParticipantPlanById;

[DocumentationInfo("GetParticipantPlanByIdQueryHandler",
    "Returns a single active participant plan by its primary key. Returns null when not found.")]
public sealed class GetParticipantPlanByIdQueryHandler
    : AizenQueryHandler<GetParticipantPlanByIdQuery, ParticipantPlanDto>
{
    private readonly IParticipantPlanRepository _plans;

    public GetParticipantPlanByIdQueryHandler(IParticipantPlanRepository plans)
        => _plans = plans;

    public override async Task<ParticipantPlanDto?> Handle(
        GetParticipantPlanByIdQuery request, CancellationToken ct)
    {
        var p = await _plans.GetByIdAsync(request.Id, ct);
        if (p is null) return null;

        return new ParticipantPlanDto(
            p.Id, p.PlanCode, p.Name, p.Description,
            p.MonthlyPriceTRY, p.AnnualPriceTRY, p.TrialDays, p.BadgeLabel,
            p.ServiceDiscountRate, p.CargoDryDiscountRate,
            p.InkCoinEarnMultiplier, p.IsFree, p.SortOrder,
            p.ValidFrom, p.ValidTo, p.FeatureItems);
    }
}
