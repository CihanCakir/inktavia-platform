using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Application.Dto;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetCommissionRuleStats;

[DocumentationInfo("GetCommissionRuleStatsQueryHandler",
    "Returns KPI counts for the admin commission rules dashboard strip: " +
    "total, active, emergency-priority, global base rate, scheduled, draft.")]
public sealed class GetCommissionRuleStatsQueryHandler
    : AizenQueryHandler<GetCommissionRuleStatsQuery, CommissionRuleStatsDto>
{
    private readonly ICommissionRuleRepository _rules;

    public GetCommissionRuleStatsQueryHandler(ICommissionRuleRepository rules)
        => _rules = rules;

    public override async Task<CommissionRuleStatsDto?> Handle(
        GetCommissionRuleStatsQuery request, CancellationToken ct)
    {
        var stats = await _rules.GetStatsAsync(ct);
        return new CommissionRuleStatsDto(
            stats.TotalRules,
            stats.ActiveRules,
            stats.EmergencyRules,
            stats.GlobalBaseRate,
            stats.ScheduledRules,
            stats.DraftRules
        );
    }
}
