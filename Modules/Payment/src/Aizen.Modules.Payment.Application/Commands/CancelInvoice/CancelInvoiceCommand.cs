using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.Payment.Application.Commands.CancelInvoice;

/// <summary>
/// Cancels a Draft invoice.
/// Only Draft invoices can be cancelled — Issued or later require a CreditNote instead.
/// Cancelled invoices never consume a sequence number (no InvoiceNumber was assigned).
/// </summary>
public sealed class CancelInvoiceCommand : AizenCommand<CancelInvoiceResult>
{
    public required long    InvoiceId          { get; init; }
    public          long?   CancelledByUserId  { get; init; }
    public          string? Reason             { get; init; }
}
