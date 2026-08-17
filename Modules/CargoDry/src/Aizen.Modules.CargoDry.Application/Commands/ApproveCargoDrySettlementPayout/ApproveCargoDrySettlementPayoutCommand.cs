using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.CargoDry.Application.Commands.ApproveCargoDrySettlementPayout;

/// <summary>
/// Approves the payout record for a CargoDry sell-through settlement,
/// transitioning the PayoutRecord from Pending → Approved.
///
/// Settlement status remains Scheduled — only CompleteCargoDrySettlementPayoutCommand
/// can advance the settlement to Settled.
///
/// Requires: settlement.Status == Scheduled, PayoutRecordId.HasValue, payout.Status == Pending.
/// Phase 4D (July 2026).
/// </summary>
[DocumentationInfo("Approve CargoDry settlement payout command",
    "Transitions the linked PayoutRecord from Pending to Approved. " +
    "Settlement remains in Scheduled status — does not advance to Settled. " +
    "No Iyzico call. No bank transfer. Phase 4D (July 2026).")]
public sealed class ApproveCargoDrySettlementPayoutCommand
    : AizenCommand<ApproveCargoDrySettlementPayoutResponse>
{
    /// <summary>Id of the CargoDrySellThroughSettlementEntity to approve payout for.</summary>
    public required long   SettlementId      { get; init; }

    /// <summary>Admin user approving the payout. Required for audit trail.</summary>
    public required long   ApprovedByUserId  { get; init; }

    /// <summary>Optional note to attach to the payout record.</summary>
    public string?         Note              { get; init; }
}

public sealed class ApproveCargoDrySettlementPayoutResponse
{
    public CargoDrySellThroughSettlementDto Settlement   { get; init; } = default!;
    public CargoDryPayoutLifecycleResultDto  PayoutResult { get; init; } = default!;
}
