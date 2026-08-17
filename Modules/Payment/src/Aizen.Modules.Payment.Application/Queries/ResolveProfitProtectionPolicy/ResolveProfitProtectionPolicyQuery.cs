using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Application.Queries.ResolveProfitProtectionPolicy;

/// <summary>Admin/dev: resolves the single active profit-protection policy for a currency (defaults to now).</summary>
public sealed class ResolveProfitProtectionPolicyQuery : AizenQuery<ProfitProtectionPolicyResult>
{
    public string    CurrencyCode { get; init; } = "TRY";
    public DateTime? AtUtc        { get; init; }
}

public sealed record ProfitProtectionPolicyResult(
    long                            PolicyId,
    string?                         PolicyCode,
    string                          CurrencyCode,
    decimal                         MinCustomerSideContributionAmount,
    decimal                         MinCustomerSideContributionRate,
    decimal                         MinProviderSideContributionAmount,
    decimal                         MinProviderSideContributionRate,
    decimal                         MinTransactionContributionAmount,
    decimal                         MinTransactionContributionRate,
    decimal                         PaymentProcessingExpenseRate,
    decimal                         PaymentProcessingFixed,
    decimal                         RefundRiskReserveRate,
    decimal                         OtherVariableExpenseRate,
    decimal                         OtherVariableExpenseFixed,
    decimal                         CustomerSideVariableCostShareRate,
    ProfitProtectionAdjustmentOrder AdjustmentOrder,
    DateTime                        EffectiveFrom,
    DateTime?                       EffectiveTo);
