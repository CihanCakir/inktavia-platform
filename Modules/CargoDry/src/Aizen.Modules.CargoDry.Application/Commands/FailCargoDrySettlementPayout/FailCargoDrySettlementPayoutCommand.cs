using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.CargoDry.Application.Commands.FailCargoDrySettlementPayout;

/// <summary>
/// Records a payout failure for a CargoDry sell-through settlement.
/// The settlement status remains Scheduled after this call — allowing the admin
/// to retry the payout after resolving the failure reason.
///
/// FailureReason is required — must describe why the payout failed.
/// Does NOT mark the settlement as Settled.
/// Does NOT cancel the settlement.
/// Does NOT call Iyzico or initiate any gateway transfer.
/// Phase 4D (July 2026).
/// </summary>
[DocumentationInfo("Fail CargoDry settlement payout command",
    "Records payout failure on the PayoutRecord and the settlement. " +
    "Settlement remains in Scheduled status — allows retry. " +
    "FailureReason required. Does NOT mark settlement as Settled or Cancelled. " +
    "No Iyzico call. Phase 4D (July 2026).")]
public sealed class FailCargoDrySettlementPayoutCommand
    : AizenCommand<FailCargoDrySettlementPayoutResponse>
{
    /// <summary>Id of the CargoDrySellThroughSettlementEntity to record payout failure for.</summary>
    public required long   SettlementId      { get; init; }

    /// <summary>Admin user recording the payout failure. Required for audit trail.</summary>
    public required long   FailedByUserId    { get; init; }

    /// <summary>
    /// Human-readable description of why the payout failed.
    /// REQUIRED — a failed payout must have a recorded failure reason.
    /// </summary>
    public required string FailureReason     { get; init; }

    /// <summary>Optional external reference (e.g. bank rejection code, error identifier).</summary>
    public string?         ExternalReference { get; init; }

    /// <summary>Optional additional note to attach to the payout record.</summary>
    public string?         Note              { get; init; }
}

public sealed class FailCargoDrySettlementPayoutResponse
{
    public CargoDrySellThroughSettlementDto Settlement   { get; init; } = default!;
    public CargoDryPayoutLifecycleResultDto  PayoutResult { get; init; } = default!;
}
