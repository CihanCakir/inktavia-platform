using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.CargoDry.Application.Commands.MarkCargoDrySettlementPayoutProcessing;

/// <summary>
/// Marks the payout record for a CargoDry sell-through settlement as Processing,
/// transitioning the PayoutRecord from Approved (or Pending) → Processing.
///
/// Settlement status remains Scheduled — only CompleteCargoDrySettlementPayoutCommand
/// can advance the settlement to Settled.
///
/// Optional — an admin may skip this step and go directly to Complete or Fail.
/// Phase 4D (July 2026).
/// </summary>
[DocumentationInfo("Mark CargoDry settlement payout processing command",
    "Transitions the linked PayoutRecord to Processing status. " +
    "Optional step between Approve and Complete. " +
    "Settlement remains in Scheduled status — does not advance to Settled. " +
    "No Iyzico call. No bank transfer. Phase 4D (July 2026).")]
public sealed class MarkCargoDrySettlementPayoutProcessingCommand
    : AizenCommand<MarkCargoDrySettlementPayoutProcessingResponse>
{
    /// <summary>Id of the CargoDrySellThroughSettlementEntity to mark payout processing for.</summary>
    public required long   SettlementId       { get; init; }

    /// <summary>Admin user marking the payout as processing. Required for audit trail.</summary>
    public required long   ProcessedByUserId  { get; init; }

    /// <summary>Optional external reference (e.g. bank transfer tracking code).</summary>
    public string?         ExternalReference  { get; init; }

    /// <summary>Optional note to attach to the payout record.</summary>
    public string?         Note               { get; init; }
}

public sealed class MarkCargoDrySettlementPayoutProcessingResponse
{
    public CargoDrySellThroughSettlementDto Settlement   { get; init; } = default!;
    public CargoDryPayoutLifecycleResultDto  PayoutResult { get; init; } = default!;
}
