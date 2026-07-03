using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Commands.PrepareCargoDrySettlementInvoice;

/// <summary>
/// Prepares a ProviderSettlementStatement invoice draft for a CargoDry sell-through settlement
/// that is in Scheduled status (has an existing PayoutRecord from Phase 4B).
///
/// Flow:
///   1. Loads the settlement and verifies it is in Scheduled status.
///   2. Idempotency check — if InvoiceId is already set, returns existing data without re-creating.
///   3. Calls ICargoDrySettlementInvoiceService.PrepareSettlementStatementAsync()
///      which dispatches an in-process MediatR command to the Payment module.
///   4. Calls settlement.MarkInvoicePrepared() to record the InvoiceId.
///      Settlement status remains Scheduled (Option B lifecycle — no InvoicePrepared enum value).
///
/// Does NOT issue the invoice. Does NOT create a PaymentTransaction.
/// Does NOT call Iyzico or execute any real money transfer.
/// Does NOT mark settlement as Settled.
///
/// Phase 4C (July 2026): CargoDry Settlement Invoice Preparation.
/// </summary>
[DocumentationInfo("Prepare CargoDry settlement invoice command",
    "Creates a Draft ProviderSettlementStatement invoice in the Payment module for a Scheduled " +
    "CargoDry sell-through settlement. Settlement status remains Scheduled after preparation. " +
    "Idempotent — if an invoice already exists, returns existing data. " +
    "Phase 4C (July 2026).")]
public sealed class PrepareCargoDrySettlementInvoiceCommand
    : AizenCommand<PrepareCargoDrySettlementInvoiceResponse>
{
    /// <summary>Id of the CargoDrySellThroughSettlementEntity to prepare invoice for.</summary>
    public required long    SettlementId      { get; init; }

    /// <summary>Admin user triggering the invoice preparation. Required for audit trail.</summary>
    public required long    PreparedByUserId  { get; init; }

    /// <summary>Optional note to attach to the settlement record.</summary>
    public string?          PreparationNote   { get; init; }
}

public sealed class PrepareCargoDrySettlementInvoiceResponse
{
    public CargoDrySellThroughSettlementDto Settlement     { get; init; } = default!;
    public long                             InvoiceId      { get; init; }
    public bool                             AlreadyExisted { get; init; }
}
