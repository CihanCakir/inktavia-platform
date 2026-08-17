using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Commands.PrepareCargoDrySettlementPayment;

/// <summary>
/// Prepares payout payment for a CargoDry sell-through settlement:
///   1. Loads settlement and verifies it is in ReadyForSettlement status.
///   2. Checks idempotency — if PayoutRecordId is already set, returns existing data.
///   3. Calls ICargoDrySettlementPayoutService.PrepareSettlementPayoutAsync()
///      which dispatches an in-process MediatR command to the Payment module.
///   4. Calls settlement.MarkPaymentPrepared() to record the PayoutRecordId
///      and transition the settlement to Scheduled status.
///
/// Does NOT create a PaymentTransaction.
/// Does NOT create an Invoice (deferred to Phase 4C — no suitable InvoiceType exists yet).
/// Does NOT call Iyzico or execute any real money transfer.
/// Does NOT implement any scheduled job.
///
/// Phase 4B (July 2026): CargoDry Settlement Payment Preparation.
/// </summary>
[DocumentationInfo("Prepare CargoDry settlement payment command",
    "Creates a payout preparation record in the Payment module for a ReadyForSettlement " +
    "CargoDry sell-through settlement and transitions the settlement to Scheduled status. " +
    "Idempotent — if a payout record already exists, returns existing data without re-creating. " +
    "Phase 4B (July 2026).")]
public sealed class PrepareCargoDrySettlementPaymentCommand
    : AizenCommand<PrepareCargoDrySettlementPaymentResponse>
{
    /// <summary>Id of the CargoDrySellThroughSettlementEntity to prepare payment for.</summary>
    public required long    SettlementId      { get; init; }

    /// <summary>Admin user triggering the payment preparation. Required for audit trail.</summary>
    public required long    PreparedByUserId  { get; init; }

    /// <summary>Optional note to attach to the settlement record.</summary>
    public string?          PreparationNote   { get; init; }
}

public sealed class PrepareCargoDrySettlementPaymentResponse
{
    public CargoDrySellThroughSettlementDto Settlement     { get; init; } = default!;
    public long                             PayoutRecordId { get; init; }
    public bool                             AlreadyExisted { get; init; }
}
