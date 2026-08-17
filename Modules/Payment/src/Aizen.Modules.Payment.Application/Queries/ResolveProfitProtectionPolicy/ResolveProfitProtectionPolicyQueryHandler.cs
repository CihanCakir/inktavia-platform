using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.ResolveProfitProtectionPolicy;

[DocumentationInfo("ResolveProfitProtectionPolicyQueryHandler",
    "Resolves the single active profit-protection policy for a currency/instant. Throws ProfitProtectionPolicyConflict " +
    "on overlap and ProfitProtectionPolicyNotFound when none is active.")]
public sealed class ResolveProfitProtectionPolicyQueryHandler
    : AizenQueryHandler<ResolveProfitProtectionPolicyQuery, ProfitProtectionPolicyResult>
{
    private readonly IProfitProtectionPolicyRepository _policies;

    public ResolveProfitProtectionPolicyQueryHandler(IProfitProtectionPolicyRepository policies) => _policies = policies;

    public override async Task<ProfitProtectionPolicyResult?> Handle(
        ResolveProfitProtectionPolicyQuery request, CancellationToken ct)
    {
        var atUtc = request.AtUtc ?? DateTime.UtcNow;

        var p = await _policies.ResolveAsync(request.CurrencyCode, atUtc, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.ProfitProtectionPolicyNotFound);

        return new ProfitProtectionPolicyResult(
            p.Id, p.PolicyCode, p.CurrencyCode,
            p.MinCustomerSideContributionAmount, p.MinCustomerSideContributionRate,
            p.MinProviderSideContributionAmount, p.MinProviderSideContributionRate,
            p.MinTransactionContributionAmount,  p.MinTransactionContributionRate,
            p.PaymentProcessingExpenseRate, p.PaymentProcessingFixed,
            p.RefundRiskReserveRate,
            p.OtherVariableExpenseRate, p.OtherVariableExpenseFixed,
            p.CustomerSideVariableCostShareRate,
            p.AdjustmentOrder, p.EffectiveFrom, p.EffectiveTo);
    }
}
