using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.CancelInvoice;

/// <summary>
/// Cancels a Draft invoice.
/// Idempotent: if already cancelled, returns current state without re-modifying.
///
/// Only Draft invoices can be cancelled via this command.
/// Issued/Sent/Paid invoices require a CreditNote (CRD document via a future CreateCreditNote command).
///
/// SaveChanges is handled by AizenCommandHandlerDecorator — NOT called here.
/// </summary>
[DocumentationInfo("Cancel invoice command handler",
    "Voids a Draft invoice. Idempotent on already-cancelled drafts. " +
    "Issued invoices cannot be cancelled — use CreateCreditNoteCommand instead.")]
public sealed class CancelInvoiceCommandHandler
    : AizenCommandHandler<CancelInvoiceCommand, CancelInvoiceResult>
{
    private readonly IInvoiceRepository                        _invoices;
    private readonly ILogger<CancelInvoiceCommandHandler>     _logger;

    public CancelInvoiceCommandHandler(
        IAizenUnitOfWork<PaymentDbContext>      unitOfWork,
        IInvoiceRepository                      invoices,
        ILogger<CancelInvoiceCommandHandler>   logger)
    {
        _invoices = invoices;
        _logger   = logger;
    }

    public override async Task<CancelInvoiceResult?> Handle(
        CancelInvoiceCommand request, CancellationToken ct)
    {
        var invoice = await _invoices.GetByIdAsync(request.InvoiceId, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.InvoiceNotFound);

        // ── Idempotency ───────────────────────────────────────────────────────
        if (invoice.Status == InvoiceStatus.Cancelled)
        {
            _logger.LogInformation(
                "CancelInvoice: invoice {Id} already cancelled at {At}. Idempotent return.",
                invoice.Id, invoice.CancelledAtUtc);
            return new CancelInvoiceResult(
                invoice.Id,
                InvoiceStatus.Cancelled,
                invoice.CancelledAtUtc!.Value);
        }

        // ── State guard ───────────────────────────────────────────────────────
        if (invoice.Status != InvoiceStatus.Draft)
            throw new AizenBusinessException((int)PaymentErrorCode.InvoiceInvalidStateTransition);

        // ── Domain transition ─────────────────────────────────────────────────
        invoice.Cancel(request.CancelledByUserId, request.Reason);
        _invoices.Update(invoice);
        // SaveChanges handled by decorator.

        _logger.LogInformation(
            "Invoice cancelled. Id={Id} Reason={Reason}",
            invoice.Id, request.Reason ?? "(none)");

        return new CancelInvoiceResult(
            invoice.Id,
            InvoiceStatus.Cancelled,
            invoice.CancelledAtUtc!.Value);
    }
}
