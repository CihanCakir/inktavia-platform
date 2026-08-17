using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Services;
using Aizen.Modules.Payment.Domain.Entities.Commission;
using Aizen.Modules.Payment.Domain.Entities.CommissionBenefit;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.ResolveEffectiveCommission;

/// <summary>
/// Admin/dev what-if: resolves the BE-P2 base commission then layers P7 benefits → the effective rate + benefit cost.
/// </summary>
public sealed class ResolveEffectiveCommissionQuery : AizenQuery<EffectiveCommissionResolution>
{
    public required long    ProviderProfileId    { get; init; }
    public long?            ProviderPlanId       { get; init; }
    public string?          CategoryCode         { get; init; }
    public required decimal ServiceAmount        { get; init; }
    public string           CurrencyCode         { get; init; } = "TRY";
    public decimal?         EligibleGmvRemaining { get; init; }
    /// <summary>Plan/system floor (e.g. 0.09 for PREMIUM_PARTNER, 0 otherwise). Control 8.</summary>
    public decimal          PlanFloorRate        { get; init; }
}

[DocumentationInfo("ResolveEffectiveCommissionQueryHandler",
    "Resolves base commission (BE-P2) then the effective rate + benefit cost (BE-P7 two-stage). Throws " +
    "CommissionRuleNotFound if no base rule; ProviderCommissionBenefitConflict / ProviderCommissionBelowFloor per §19.5.")]
public sealed class ResolveEffectiveCommissionQueryHandler
    : AizenQueryHandler<ResolveEffectiveCommissionQuery, EffectiveCommissionResolution>
{
    private readonly ICommissionRuleRepository _commission;
    private readonly ProviderCommissionBenefitService _benefits;

    public ResolveEffectiveCommissionQueryHandler(
        ICommissionRuleRepository commission, ProviderCommissionBenefitService benefits)
    {
        _commission = commission;
        _benefits   = benefits;
    }

    public override async Task<EffectiveCommissionResolution?> Handle(
        ResolveEffectiveCommissionQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var baseResolution = await _commission.ResolveAsync(
            new CommissionResolveContext(
                ProviderProfileId: request.ProviderProfileId,
                ProviderPlanId:    request.ProviderPlanId,
                CategoryCode:      request.CategoryCode,
                CurrencyCode:      request.CurrencyCode),
            now, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.CommissionRuleNotFound);

        var ctx = new ProviderCommissionBenefitContext(
            ProviderProfileId:    request.ProviderProfileId,
            ProviderPlanId:       request.ProviderPlanId,
            CategoryCode:         request.CategoryCode,
            ServiceAmount:        request.ServiceAmount,
            EligibleGmvRemaining: request.EligibleGmvRemaining,
            CurrencyCode:         request.CurrencyCode,
            PlanFloorRate:        request.PlanFloorRate);

        return await _benefits.ResolveEffectiveAsync(baseResolution, ctx, now, ct);
    }
}
