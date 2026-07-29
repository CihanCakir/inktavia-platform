using Aizen.Modules.Payment.Domain.Entities.Commission;
using Aizen.Modules.Payment.Domain.Entities.CommissionBenefit;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Services;

/// <summary>
/// Produces the stage-2 effective commission on top of a BE-P2 base resolution (§19.5). Fetches the candidate benefit
/// rules and delegates to the pure <see cref="ProviderCommissionBenefitResolver"/> — no writes. The resulting
/// <c>AppliedBenefitAmount</c> is P5's <c>ProviderCommissionBenefitCost</c>; the effective rate is what P8 applies.
/// The entitlement's remaining GMV is supplied via the context (computed by P8 from the granted entitlement).
/// BE-P2 base resolution is never recomputed here; premium/boost products are never read (control 1, by construction).
/// </summary>
public sealed class ProviderCommissionBenefitService
{
    private readonly IProviderCommissionBenefitRuleRepository _rules;
    public ProviderCommissionBenefitService(IProviderCommissionBenefitRuleRepository rules) => _rules = rules;

    public async Task<EffectiveCommissionResolution> ResolveEffectiveAsync(
        CommissionResolution baseResolution,
        ProviderCommissionBenefitContext ctx,
        DateTime atUtc,
        CancellationToken ct = default)
    {
        var candidates = await _rules.GetActiveCandidatesAsync(ctx.CurrencyCode, atUtc, ct);
        return ProviderCommissionBenefitResolver.ResolveEffectiveCommission(baseResolution, ctx, candidates);
    }
}
