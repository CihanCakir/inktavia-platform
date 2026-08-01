using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Dto;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetPlatformFeeRuleStats;

[DocumentationInfo("GetPlatformFeeRuleStatsQueryHandler",
    "Returns KPI counts for the admin platform fee rules dashboard strip: total, active, and per-model counts " +
    "(Percentage / Fixed / PercentageWithBounds / Waived). Computed from the full rule set (no repository change).")]
public sealed class GetPlatformFeeRuleStatsQueryHandler
    : AizenQueryHandler<GetPlatformFeeRuleStatsQuery, PlatformFeeRuleStatsDto>
{
    private readonly IPlatformFeeRuleRepository _rules;

    public GetPlatformFeeRuleStatsQueryHandler(IPlatformFeeRuleRepository rules)
        => _rules = rules;

    public override async Task<PlatformFeeRuleStatsDto?> Handle(
        GetPlatformFeeRuleStatsQuery request, CancellationToken ct)
    {
        var all = await _rules.GetAllAsync(ct);

        return new PlatformFeeRuleStatsDto(
            TotalRules:      all.Count,
            ActiveRules:     all.Count(r => r.IsActive),
            PercentageRules: all.Count(r => r.Model == PlatformFeeModel.Percentage),
            FixedRules:      all.Count(r => r.Model == PlatformFeeModel.Fixed),
            BoundsRules:     all.Count(r => r.Model == PlatformFeeModel.PercentageWithBounds),
            WaivedRules:     all.Count(r => r.Model == PlatformFeeModel.Waived));
    }
}
