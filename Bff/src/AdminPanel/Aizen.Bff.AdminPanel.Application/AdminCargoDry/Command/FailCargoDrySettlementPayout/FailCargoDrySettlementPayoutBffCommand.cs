using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.FailCargoDrySettlementPayout;

/// <summary>
/// BFF command that proxies to the CargoDry commercial module's fail-payout endpoint.
/// Records a payout failure on both the PayoutRecord and the settlement.
/// Settlement status remains Scheduled after this call, allowing the admin to retry.
/// FailureReason is required. Cannot fail an already-Settled settlement.
/// No gateway call. Phase 4D (July 2026).
/// </summary>
public sealed class FailCargoDrySettlementPayoutBffCommand
    : AizenCommand<FailCargoDrySettlementPayoutBffCommandResponse>
{
    public long    SettlementId      { get; init; }
    public long    FailedByUserId    { get; init; }
    public string  FailureReason     { get; init; } = default!;
    public string? ExternalReference { get; init; }
    public string? Note              { get; init; }
}

public sealed class FailCargoDrySettlementPayoutBffCommandResponse
{
    public CargoDrySellThroughSettlementBffDto? Settlement   { get; init; }
    public CargoDryPayoutLifecycleResultBffDto? PayoutResult { get; init; }
}
