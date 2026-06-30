using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.ResolveCommissionRate;

public sealed class ResolveCommissionRateQueryHandler
    : AizenQueryHandler<ResolveCommissionRateQuery, CommissionRateResult>
{
    private readonly ICommissionRuleRepository _rules;

    public ResolveCommissionRateQueryHandler(ICommissionRuleRepository rules)
        => _rules = rules;

    public override async Task<CommissionRateResult?> Handle(
        ResolveCommissionRateQuery request, CancellationToken ct)
    {
        var atUtc = DateTime.UtcNow;
        var rate = await _rules.ResolveRateAsync(
            request.ProviderProfileId, request.ProviderPlanId, request.CategoryCode, atUtc, ct);

        if (rate is null) return null;

        // Determine which level of the chain resolved it (for admin display)
        string source;
        if (request.ProviderProfileId.HasValue)
            source = CommissionRuleType.ProviderOverride.ToString();
        else if (request.ProviderPlanId.HasValue)
            source = CommissionRuleType.Plan.ToString();
        else if (!string.IsNullOrWhiteSpace(request.CategoryCode))
            source = CommissionRuleType.Category.ToString();
        else
            source = CommissionRuleType.Global.ToString();

        return new CommissionRateResult(rate.Value, source);
    }
}
