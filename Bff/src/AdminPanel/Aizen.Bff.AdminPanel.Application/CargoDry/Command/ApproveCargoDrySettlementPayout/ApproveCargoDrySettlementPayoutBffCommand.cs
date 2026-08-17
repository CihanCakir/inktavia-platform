using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.ApproveCargoDrySettlementPayout;

/// <summary>
/// BFF command that proxies to the CargoDry commercial module's approve-payout endpoint.
/// Transitions the linked PayoutRecord from Pending → Approved.
/// Settlement status remains Scheduled — only complete-payout advances it to Settled.
/// No gateway call. No bank transfer. Phase 4D (July 2026).
/// </summary>
public sealed class ApproveCargoDrySettlementPayoutBffCommand
    : AizenCommand<ApproveCargoDrySettlementPayoutBffCommandResponse>
{
    public long    SettlementId      { get; init; }
    public long    ApprovedByUserId  { get; init; }
    public string? Note              { get; init; }
}

public sealed class ApproveCargoDrySettlementPayoutBffCommandResponse
{
    public CargoDrySellThroughSettlementBffDto? Settlement   { get; init; }
    public CargoDryPayoutLifecycleResultBffDto? PayoutResult { get; init; }
}
