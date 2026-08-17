using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Application.Services;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.IssueInvoice;

/// <summary>
/// Issues a Draft invoice by assigning a formatted invoice number and transitioning to Issued.
///
/// ── Number generation ────────────────────────────────────────────────────────
///  InvoiceNumberService handles sequence increment with up to 5 optimistic-concurrency retries.
///  The sequence SaveChanges is committed independently of the outer UnitOfWork so the
///  sequence number is not rolled back if the outer transaction fails.
///
/// ── Lines required ───────────────────────────────────────────────────────────
///  A draft with zero lines cannot be issued (error 5031).
///
/// ── SaveChanges ───────────────────────────────────────────────────────────────
///  The outer invoice update (Status → Issued, InvoiceNumber assigned) is NOT saved here.
///  AizenCommandHandlerDecorator calls SaveChangesAsync on the outer UnitOfWork.
///
/// ── Event publishing ─────────────────────────────────────────────────────────
///  InvoiceIssuedMessage is published fire-and-forget after the domain transition.
///  Consumers: Notification module (email delivery), BFF cache invalidation.
///  Delivery failure is logged but does not roll back the issuance.
/// </summary>
[DocumentationInfo("Issue invoice command handler",
    "Transitions a Draft invoice to Issued, assigning a concurrency-safe formatted invoice number. " +
    "Uses InvoiceNumberService for sequence management. Publishes InvoiceIssuedMessage.")]
public sealed class IssueInvoiceCommandHandler
    : AizenCommandHandler<IssueInvoiceCommand, IssueInvoiceResult>
{
    private readonly IInvoiceRepository                       _invoices;
    private readonly InvoiceNumberService                     _numberService;
    private readonly IAizenMessagePublisher                   _publisher;
    private readonly ILogger<IssueInvoiceCommandHandler>     _logger;

    public IssueInvoiceCommandHandler(
        IAizenUnitOfWork<PaymentDbContext>     unitOfWork,
        IInvoiceRepository                     invoices,
        InvoiceNumberService                   numberService,
        IAizenMessagePublisher                 publisher,
        ILogger<IssueInvoiceCommandHandler>   logger)
    {
        _invoices      = invoices;
        _numberService = numberService;
        _publisher     = publisher;
        _logger        = logger;
    }

    public override async Task<IssueInvoiceResult?> Handle(
        IssueInvoiceCommand request, CancellationToken ct)
    {
        // ── 1. Load invoice with lines to validate ────────────────────────────
        var invoice = await _invoices.GetByIdFullAsync(request.InvoiceId, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.InvoiceNotFound);

        // ── 2. Guard: already issued (idempotent) ─────────────────────────────
        if (invoice.Status == InvoiceStatus.Issued)
        {
            _logger.LogInformation(
                "IssueInvoice: invoice {Id} already issued ({Number}). Idempotent return.",
                invoice.Id, invoice.InvoiceNumber);
            return new IssueInvoiceResult(
                invoice.Id,
                invoice.InvoiceNumber!,
                invoice.IssueDateUtc!.Value);
        }

        // ── 3. Guard: wrong state ─────────────────────────────────────────────
        if (invoice.Status != InvoiceStatus.Draft)
            throw new AizenBusinessException((int)PaymentErrorCode.InvoiceAlreadyIssued);

        // ── 4. Guard: at least one line ───────────────────────────────────────
        if (!invoice.Lines.Any())
            throw new AizenBusinessException((int)PaymentErrorCode.InvoiceLinesRequired);

        // ── 5. Generate invoice number (commits its own SaveChanges internally) ─
        var invoiceNumber = await _numberService.GenerateAsync(invoice.InvoiceType, ct);

        // ── 6. Domain transition ──────────────────────────────────────────────
        invoice.Issue(invoiceNumber, request.IssuedByUserId);
        _invoices.Update(invoice);
        // Outer SaveChanges handled by AizenCommandHandlerDecorator.

        _logger.LogInformation(
            "Invoice issued. Id={Id} Number={Number} Type={Type}",
            invoice.Id, invoiceNumber, invoice.InvoiceType);

        // ── 7. Publish event (fire-and-forget; failure does not roll back issuance) ─
        _ = _publisher.PublishAsync(new InvoiceIssuedMessage
        {
            InvoiceId    = invoice.Id,
            InvoiceNumber = invoiceNumber,
            InvoiceType  = invoice.InvoiceType,
            BuyerUserId  = invoice.BuyerUserId,
            BuyerName    = invoice.BuyerName,
            SellerUserId = invoice.SellerUserId,
            TotalAmount  = invoice.TotalAmount,
            Currency     = invoice.Currency,
            IssuedAtUtc  = invoice.IssueDateUtc!.Value,
            SourceId     = invoice.SourceId,
            SourceType   = invoice.SourceType,
        }, ct).ContinueWith(t =>
        {
            if (t.IsFaulted)
                _logger.LogError(t.Exception,
                    "Failed to publish InvoiceIssuedMessage for InvoiceId={Id}", invoice.Id);
        }, TaskContinuationOptions.OnlyOnFaulted);

        return new IssueInvoiceResult(
            invoice.Id,
            invoiceNumber,
            invoice.IssueDateUtc!.Value);
    }
}
