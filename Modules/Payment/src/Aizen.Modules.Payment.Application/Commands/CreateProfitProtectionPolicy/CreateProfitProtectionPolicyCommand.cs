using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Application.Commands.CreateProfitProtectionPolicy;

public sealed class CreateProfitProtectionPolicyCommand : AizenCommand<CreateProfitProtectionPolicyResult>
{
    public string  CurrencyCode { get; init; } = "TRY";

    public decimal MinCustomerSideContributionAmount { get; init; }
    public decimal MinCustomerSideContributionRate   { get; init; }
    public decimal MinProviderSideContributionAmount { get; init; }
    public decimal MinProviderSideContributionRate   { get; init; }
    public decimal MinTransactionContributionAmount  { get; init; }
    public decimal MinTransactionContributionRate    { get; init; }

    public decimal PaymentProcessingExpenseRate { get; init; }
    public decimal PaymentProcessingFixed       { get; init; }
    public decimal RefundRiskReserveRate        { get; init; }
    public decimal OtherVariableExpenseRate     { get; init; }
    public decimal OtherVariableExpenseFixed    { get; init; }
    public decimal CustomerSideVariableCostShareRate { get; init; } = 0.5m;

    // ── BE-S9 line-level defaults (§20.12; admin-tunable) — caps default to 1 (100% = no-op), floors/rates 0, exception off ──
    public decimal DefaultLineMinProviderReceivableRate    { get; init; } = 0m;
    public decimal DefaultLineMinProviderReceivableAmount  { get; init; } = 0m;
    public decimal DefaultAllowedProviderFundedDiscountRate { get; init; } = 1m;
    public decimal DefaultAllowedPlatformFundedDiscountRate { get; init; } = 1m;
    public decimal LineCommissionFloorRate                 { get; init; } = 0m;
    public decimal MinLinePlatformContributionRate         { get; init; } = 0m;
    public bool    StrategicLossExceptionEnabled           { get; init; } = false;
    public decimal StrategicLossExceptionMaxLineDeficit    { get; init; } = 0m;

    public ProfitProtectionAdjustmentOrder AdjustmentOrder { get; init; }
        = ProfitProtectionAdjustmentOrder.PlatformDiscountThenCommissionBenefit;

    public required DateTime EffectiveFrom { get; init; }
    public DateTime?         EffectiveTo   { get; init; }
    public string?           PolicyName    { get; init; }
    public string?           Notes         { get; init; }
}

public sealed record CreateProfitProtectionPolicyResult(long Id, string PolicyCode);
