using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.MarkCargoDrySettlementPayoutProcessing;

/// <summary>
/// BFF command that proxies to the CargoDry commercial module's mark-payout-processing endpoint.
/// Transitions the linked PayoutRecord to Processing status (optional step between Approve and Complete).
/// Settlement status remains Scheduled. No gateway call. No bank transfer. Phase 4D (July 2026).
/// </summary>
public sealed class MarkCargoDrySettlementPayoutProcessingBffCommand
    : AizenCommand<MarkCargoDrySettlementPayoutProcessingBffCommandResponse>
{
    public long    SettlementId      { get; init; }
    public long    ProcessedByUserId { get; init; }
    public string? ExternalReference { get; init; }
    public string? Note              { get; init; }
}

public sealed class MarkCargoDrySettlementPayoutProcessingBffCommandResponse
{
    public CargoDrySellThroughSettlementBffDto? Settlement   { get; init; }
    public CargoDryPayoutLifecycleResultBffDto? PayoutResult { get; init; }
}
