using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.CargoDry.Application.Commands.CompleteCargoDrySettlementPayout;

/// <summary>
/// Records the successful completion of a manual payout for a CargoDry sell-through settlement.
/// This is the ONLY command allowed to mark the settlement as Settled.
///
/// Flow:
///   1. Loads the settlement — must be in Scheduled status with PayoutRecordId and InvoiceId.
///   2. Idempotent: if payout record is already Completed, returns existing data.
///   3. Calls ICargoDrySettlementPayoutLifecycleService.CompleteManualAsync()
///      — marks the PayoutRecord as Completed in the Payment module.
///   4. Calls settlement.MarkPayoutCompleted() — records closure fields and sets Status = Settled.
///   5. Saves the settlement.
///
/// ManualPaymentReference is required — represents the bank transfer reference, receipt,
/// or external payment confirmation identifier.
///
/// Does NOT call Iyzico or initiate any gateway transfer.
/// Phase 4D (July 2026).
/// </summary>
[DocumentationInfo("Complete CargoDry settlement payout command",
    "Records manual payout completion and closes the settlement as Settled. " +
    "ONLY command that can advance settlement status to Settled. " +
    "ManualPaymentReference required — must be the bank transfer ref or payment confirmation. " +
    "Idempotent if PayoutRecord is already Completed. " +
    "Requires Phase 4B (PayoutRecord) and Phase 4C (InvoiceId) to be complete. " +
    "No Iyzico call. No automatic transfer. Phase 4D (July 2026).")]
public sealed class CompleteCargoDrySettlementPayoutCommand
    : AizenCommand<CompleteCargoDrySettlementPayoutResponse>
{
    /// <summary>Id of the CargoDrySellThroughSettlementEntity to complete payout for.</summary>
    public required long   SettlementId             { get; init; }

    /// <summary>Admin user recording the payout completion. Required for audit trail.</summary>
    public required long   CompletedByUserId         { get; init; }

    /// <summary>
    /// Bank transfer reference, receipt number, or external payment confirmation identifier.
    /// REQUIRED — a completed payout must have a traceable external reference.
    /// </summary>
    public required string ManualPaymentReference    { get; init; }

    /// <summary>Optional note to attach to the payout and settlement records.</summary>
    public string?         Note                     { get; init; }
}

public sealed class CompleteCargoDrySettlementPayoutResponse
{
    public CargoDrySellThroughSettlementDto Settlement    { get; init; } = default!;
    public CargoDryPayoutLifecycleResultDto  PayoutResult { get; init; } = default!;
    /// <summary>True if the payout was already completed in a prior call (idempotent return).</summary>
    public bool                              AlreadyCompleted { get; init; }
}
