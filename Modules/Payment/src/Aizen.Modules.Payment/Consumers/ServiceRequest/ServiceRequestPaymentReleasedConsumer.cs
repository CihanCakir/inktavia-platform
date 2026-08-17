using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Message;
using Aizen.Modules.Payment.Application.Services;
using Aizen.Modules.Payment.Domain.Entities.Invoice;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Consumers.ServiceRequest;

/// <summary>
/// Listens for PaymentEscrowReleasedMessage (published by ServiceRequestCompletedConsumer).
/// Automatically creates and issues a CommissionInvoice documenting Inktavia's commission
/// from the service request payment.
///
/// ── Invoice structure ─────────────────────────────────────────────────────────
///  Seller : Inktavia Marine OS (platform — SellerUserId = null)
///  Buyer  : Provider (identified by RecipientProfileId; name resolved as "Provider #{id}" MVP placeholder)
///  Source : ServiceRequest (ContextId from message)
///  Lines  : Single line — "Platform Commission" at CommissionAmount (gross incl. VAT)
///
/// ── BuyerName enrichment (TODO post-MVP) ─────────────────────────────────────
///  The provider's display name is not available in PaymentEscrowReleasedMessage.
///  MVP uses "Provider #{RecipientProfileId}" as a placeholder.
///  Post-MVP: inject an IIdentityProviderNameResolver cross-module service or add
///  ProviderDisplayName to PaymentEscrowReleasedMessage at the publish site.
///
/// ── Tax handling ─────────────────────────────────────────────────────────────
///  CommissionAmount in PaymentEscrowReleasedMessage is the GROSS commission (net + VAT).
///  To reconstruct the KDV breakdown, the consumer reads Payment:DefaultKdvRate (default 0.20).
///  TaxableAmount = round(CommissionAmount / (1 + KdvRate), 4)
///  TaxAmount     = CommissionAmount - TaxableAmount
///
/// ── Idempotency ───────────────────────────────────────────────────────────────
///  Key: "COM-INV-TX-{TransactionId}"
///  If a CommissionInvoice already exists for this TransactionId, the consumer skips.
///
/// ── SaveChanges ───────────────────────────────────────────────────────────────
///  Consumers are NOT wrapped by AizenCommandHandlerDecorator — must call SaveChangesAsync directly.
/// </summary>
public sealed class ServiceRequestPaymentReleasedConsumer
    : AizenBaseMessageConsumer<PaymentEscrowReleasedMessage>
{
    private readonly IInvoiceRepository                                  _invoices;
    private readonly InvoiceNumberService                                _numberService;
    private readonly IAizenMessagePublisher                              _publisher;
    private readonly IConfiguration                                      _config;
    private readonly ILogger<ServiceRequestPaymentReleasedConsumer>      _logger;

    public ServiceRequestPaymentReleasedConsumer(IServiceProvider sp) : base(sp)
    {
        _invoices      = sp.GetRequiredService<IInvoiceRepository>();
        _numberService = sp.GetRequiredService<InvoiceNumberService>();
        _publisher     = sp.GetRequiredService<IAizenMessagePublisher>();
        _config        = sp.GetRequiredService<IConfiguration>();
        _logger        = sp.GetRequiredService<ILogger<ServiceRequestPaymentReleasedConsumer>>();
    }

    public override async Task<bool> ExecutePrepareMessage(
        PaymentEscrowReleasedMessage message, CancellationToken ct)
    {
        // Only handle ServiceRequest context
        if (message.ContextType != TransactionContextType.ServiceRequest)
        {
            _logger.LogDebug(
                "ServiceRequestPaymentReleasedConsumer: skipping non-SR context {ContextType}",
                message.ContextType);
            return false;
        }

        // Idempotency: check if CommissionInvoice already exists for this transaction
        var existing = await _invoices.GetByTransactionIdAsync(message.TransactionId, ct);
        if (existing is not null && existing.InvoiceType == InvoiceType.CommissionInvoice)
        {
            _logger.LogInformation(
                "ServiceRequestPaymentReleasedConsumer: CommissionInvoice already exists " +
                "(InvoiceId={Id}) for TxId={TxId}. Idempotent skip.",
                existing.Id, message.TransactionId);
            return false;
        }

        if (message.CommissionAmount <= 0m)
        {
            _logger.LogInformation(
                "ServiceRequestPaymentReleasedConsumer: CommissionAmount=0 for TxId={TxId}. " +
                "No commission invoice needed (zero-commission plan or free tier).",
                message.TransactionId);
            return false;
        }

        return true;
    }

    public override async Task ExecuteCommitMessage(
        PaymentEscrowReleasedMessage message, CancellationToken ct)
    {
        // ── WS1 PART C: commit-level idempotency (mirror of the partial-unique DB index) ──────
        // A duplicate two-phase commit copy of this event must not issue a second CommissionInvoice /
        // invoice number / InvoiceIssuedMessage (commission revenue). Re-check the natural key here.
        if (await _invoices.ExistsByTransactionIdAndTypeAsync(
                message.TransactionId, InvoiceType.CommissionInvoice, ct))
        {
            _logger.LogInformation(
                "ServiceRequestPaymentReleasedConsumer: CommissionInvoice already exists for TxId={TxId}. " +
                "Commit idempotent skip.", message.TransactionId);
            return;
        }

        var kdvRate = _config.GetValue<decimal>("Payment:DefaultKdvRate", 0.20m);

        // ── Reconstruct line amounts from gross CommissionAmount ──────────────
        var grossAmount   = message.CommissionAmount;
        var taxableAmount = Math.Round(grossAmount / (1m + kdvRate), 4, MidpointRounding.AwayFromZero);
        var taxAmount     = Math.Round(grossAmount - taxableAmount, 4, MidpointRounding.AwayFromZero);

        // ── Build buyer name (MVP placeholder — post-MVP enrich from Identity) ─
        var buyerName = $"Provider #{message.RecipientProfileId}";

        // ── Create draft invoice ──────────────────────────────────────────────
        var invoice = InvoiceHeaderEntity.CreateDraft(
            invoiceType:           InvoiceType.CommissionInvoice,
            commercialModel:       CommercialModel.MarketplaceCommission,
            sourceType:            InvoiceSourceType.ServiceRequest,
            sourceId:              message.ContextId,
            sellerName:            "Inktavia Marine OS",
            buyerName:             buyerName,
            currency:              message.CurrencyCode,
            subTotalAmount:        taxableAmount,
            discountAmount:        0m,
            taxableAmount:         taxableAmount,
            taxAmount:             taxAmount,
            totalAmount:           grossAmount,
            sellerUserId:          null,
            buyerUserId:           message.RecipientProfileId,
            paymentTransactionId:  message.TransactionId,
            paymentReleaseId:      null,
            notes: $"Auto-generated commission invoice for SR #{message.ContextId}. " +
                   $"Gateway payout: {message.GatewayPayoutId ?? "N/A"}.");

        // ── Add commission line ───────────────────────────────────────────────
        var line = InvoiceLineEntity.Create(
            invoiceHeaderId:      0,   // EF cascade assign
            lineNumber:           1,
            lineType:             InvoiceLineType.ServiceFee,
            description:          $"Inktavia Platform Commission — SR #{message.ContextId}",
            productCode:          null,
            serviceCategoryCode:  "PLATFORM_COMMISSION",
            quantity:             1m,
            unitCode:             "EACH",
            unitPrice:            taxableAmount,
            discountAmount:       0m,
            taxRate:              kdvRate,
            sourceType:           null,
            sourceId:             null);

        invoice.AddLine(line);

        var breakdown = InvoiceTaxBreakdownEntity.Create(
            invoiceHeaderId: 0,   // EF cascade assign
            taxType:         $"KDV{(int)Math.Round(kdvRate * 100)}",
            taxRate:         kdvRate,
            taxableAmount:   taxableAmount);

        invoice.AddTaxBreakdown(breakdown);

        await _invoices.AddAsync(invoice, ct);

        // ── Issue immediately: assign formatted number ────────────────────────
        var invoiceNumber = await _numberService.GenerateAsync(InvoiceType.CommissionInvoice, ct);
        invoice.Issue(invoiceNumber, issuedByUserId: null);
        _invoices.Update(invoice);

        // Consumers must call SaveChangesAsync directly — no decorator wrapping.
        // WS1: partial-unique index on (PaymentTransactionId, InvoiceType) is the race backstop.
        try
        {
            await _invoices.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (PaymentIdempotency.IsUniqueViolation(ex))
        {
            _logger.LogWarning(
                "ServiceRequestPaymentReleasedConsumer: concurrent commit already issued a CommissionInvoice " +
                "for TxId={TxId} (unique-violation swallowed). Idempotent skip.", message.TransactionId);
            return;
        }

        _logger.LogInformation(
            "CommissionInvoice auto-issued. Number={Number} TxId={TxId} SR={SrId} Amount={Amount} {Currency}",
            invoiceNumber, message.TransactionId, message.ContextId, grossAmount, message.CurrencyCode);

        // ── Publish InvoiceIssuedMessage ──────────────────────────────────────
        _ = _publisher.PublishAsync(new InvoiceIssuedMessage
        {
            InvoiceId     = invoice.Id,
            InvoiceNumber = invoiceNumber,
            InvoiceType   = InvoiceType.CommissionInvoice,
            BuyerUserId   = message.RecipientProfileId,
            BuyerName     = buyerName,
            SellerUserId  = null,
            TotalAmount   = grossAmount,
            Currency      = message.CurrencyCode,
            IssuedAtUtc   = invoice.IssueDateUtc!.Value,
            SourceId      = message.ContextId,
            SourceType    = InvoiceSourceType.ServiceRequest,
        }, ct).ContinueWith(t =>
        {
            if (t.IsFaulted)
                _logger.LogError(t.Exception,
                    "Failed to publish InvoiceIssuedMessage for CommissionInvoice {InvoiceId}",
                    invoice.Id);
        }, TaskContinuationOptions.OnlyOnFaulted);
    }

    public override Task ExecuteRollbackMessage(
        PaymentEscrowReleasedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogError(
            "ServiceRequestPaymentReleasedConsumer rollback for TxId={TxId} SR={SrId}: {Error}",
            message.TransactionId, message.ContextId, ex.Message);
        return Task.CompletedTask;
    }
}
