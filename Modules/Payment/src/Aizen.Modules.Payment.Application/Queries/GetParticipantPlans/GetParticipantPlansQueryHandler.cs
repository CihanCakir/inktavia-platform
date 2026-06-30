using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetParticipantPlans;

public sealed class GetParticipantPlansQueryHandler
    : AizenQueryHandler<GetParticipantPlansQuery, List<ParticipantPlanDto>>
{
    private readonly IParticipantPlanRepository _plans;

    public GetParticipantPlansQueryHandler(IParticipantPlanRepository plans)
        => _plans = plans;

    public override async Task<List<ParticipantPlanDto>?> Handle(
        GetParticipantPlansQuery request, CancellationToken ct)
    {
        var plans = await _plans.GetAllActiveAsync(ct);
        return plans.Select(p => new ParticipantPlanDto(
            p.Id, p.PlanCode, p.Name, p.MonthlyPriceTRY,
            p.ServiceDiscountRate, p.CargoDryDiscountRate,
            p.InkCoinEarnMultiplier, p.SortOrder)).ToList();
    }
}
