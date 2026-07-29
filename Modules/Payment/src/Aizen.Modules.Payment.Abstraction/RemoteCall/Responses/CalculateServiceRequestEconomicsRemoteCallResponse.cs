using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;

/// <summary>
/// BE-P8 acceptance-economics result. On <see cref="ProfitProtectionDecisionState.Approved"/> /
/// <see cref="ProfitProtectionDecisionState.ApprovedWithAdjustment"/>: the escrow transaction + immutable snapshot were
/// created (<see cref="TransactionId"/> / <see cref="EconomicsSnapshotId"/> set) and SR may commit acceptance using these
/// amounts. On <see cref="ProfitProtectionDecisionState.Rejected"/> / <see cref="ProfitProtectionDecisionState.ConfigurationError"/>:
/// no snapshot, no escrow — SR MUST block acceptance and surface <see cref="Reason"/>.
/// </summary>
public sealed class CalculateServiceRequestEconomicsRemoteCallResponse
{
    public required ProfitProtectionDecisionState Decision { get; init; }
    public string? Reason { get; init; }

    /// <summary>
    /// BE-I1 — false when the recipient provider is not split-eligible (no sub-merchant). A hard block: the P8 handler
    /// creates no snapshot/escrow and the SR handler blocks acceptance with <c>ProviderNotSplitEligible</c>. Defaults true.
    /// </summary>
    public bool ProviderSplitEligible { get; init; } = true;

    public long?   TransactionId          { get; init; }
    public long?   EconomicsSnapshotId    { get; init; }
    public long?   ResolvedProviderPlanId { get; init; }

    public decimal CustomerTotalAmount    { get; init; }   // = escrow gross
    public decimal ProviderNetTotal       { get; init; }   // = escrow split target
    public decimal PlatformFeeGross       { get; init; }
    public decimal TransactionCommission  { get; init; }

    public bool CanProceed => Decision is ProfitProtectionDecisionState.Approved
                                       or ProfitProtectionDecisionState.ApprovedWithAdjustment;
}
