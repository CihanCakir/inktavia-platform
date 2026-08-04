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

namespace Aizen.Modules.Payment.Consumers.Subscription;

/// <summary>
/// Listens for PaymentCapturedMessage where ContextType=Subscription and the referenced plan
/// is a ProviderPlanEntity. Activates the provider's subscription and auto-issues a
/// SubscriptionInvoice.
///
/// ── Discrimination strategy ───────────────────────────────────────────────────
///  Both ProviderSubscriptionPaymentSucceededConsumer and
///  ParticipantSubscriptionPaymentSucceededConsumer receive every
///  PaymentCapturedMessage(ContextType=Subscription).
///  ExecutePrepareMessage routes by looking up ContextId in the respective plan table:
///  - If ProviderPlanEntity exists → this consumer handles it.
///  - If ParticipantPlanEntity exists → the other consumer handles it.
///  One consumer always returns false; no double-processing occurs.
///
/// ── Context mapping ───────────────────────────────────────────────────────────
///  ContextId       = ProviderPlanEntity.Id
///  PayerProfileId  = ProviderProfileId (provider pays for their subscription)
///  GrossAmount     = plan monthly price paid
///
/// ── Idempotency ───────────────────────────────────────────────────────────────
///  Prepare checks: no existing invoice for this TransactionId (GetByTransactionIdAsync).
///  Commit: if subscription was already created (retry after invoice failure),
///          it is reused via GetSubscriptionByTransactionIdAsync before re-creating.
///
/// ── SaveChanges ───────────────────────────────────────────────────────────────
///  Two explicit SaveChanges calls:
///  1. After subscription creation — needed to obtain subscription.Id for invoice SourceId.
///  2. After invoice creation + issuance.
///
/// ── IAP (Apple / Google) ─────────────────────────────────────────────────────
///  Provider subscriptions are always web/card via Iyzico.
///  Apple/Google IAP paths apply to Participant subscriptions only (Phase 2C).
///
/// Configuration:
///   Payment:DefaultKdvRate  (default 0.20)
///   Payment:SubscriptionDueDays  (default 7 — DueDateUtc offset from issue date)
/// </summary>
public sealed class ProviderSubscriptionPaymentSucceededConsumer
    : AizenBaseMessageConsumer<PaymentCapturedMessage>
{
    private readonly IProviderPlanRepository                                  _providerPlans;
    private readonly IInvoiceRepository                                       _invoices;
    private readonly InvoiceNumberService                                     _numberService;
    private readonly IAizenMessagePublisher                                   _publisher;
    private readonly IConfiguration                                           _config;
    private readonly ILogger<ProviderSubscriptionPaymentSucceededConsumer>    _logger;

    public ProviderSubscriptionPaymentSucceededConsumer(IServiceProvider sp) : base(sp)
    {
        _providerPlans = sp.GetRequiredService<IProviderPlanRepository>();
        _invoices      = sp.GetRequiredService<IInvoiceRepository>();
        _numberService = sp.GetRequiredService<InvoiceNumberService>();
        _publisher     = sp.GetRequiredService<IAizenMessagePublisher>();
        _config        = sp.GetRequiredService<IConfiguration>();
        _logger        = sp.GetRequiredService<ILogger<ProviderSubscriptionPaymentSucceededConsumer>>();
    }

    public override async Task<bool> ExecutePrepareMessage(
        PaymentCapturedMessage message, CancellationToken ct)
    {
        // Only handle subscription payments
        if (message.ContextType != TransactionContextType.Subscription)
            return false;

        // Discriminate: is ContextId a ProviderPlan?
        var plan = await _providerPlans.GetByIdAsync(message.ContextId, ct);
        if (plan is null)
        {
            // Not a provider plan — ParticipantSubscriptionPaymentSucceededConsumer will handle this.
            return false;
        }

        // Idempotency: invoice already issued for this transaction?
        var existingInvoice = await _invoices.GetByTransactionIdAsync(message.TransactionId, ct);
        if (existingInvoice is not null)
        {
            _logger.LogInformation(
                "ProviderSubscriptionPaymentSucceededConsumer: SubscriptionInvoice already exists " +
                "(InvoiceId={InvoiceId}) for TxId={TxId}. Idempotent skip.",
                existingInvoice.Id, message.TransactionId);
            return false;
        }

        return true;
    }

    public override async Task ExecuteCommitMessage(
        PaymentCapturedMessage message, CancellationToken ct)
    {
        // ── WS1 PART C: commit-level idempotency (mirror of the partial-unique DB index) ──────
        // The two-phase double-commit can deliver a second commit copy of this event. The Prepare guard
        // only gates *this* consumer's own commit publish; the duplicate commit was published by another
        // same-message consumer and reaches this queue regardless. Re-check the natural key here so the
        // duplicate returns without issuing a second SubscriptionInvoice / invoice number / outbound event.
        if (await _invoices.ExistsByTransactionIdAndTypeAsync(
                message.TransactionId, InvoiceType.SubscriptionInvoice, ct))
        {
            _logger.LogInformation(
                "ProviderSubscriptionPaymentSucceededConsumer: SubscriptionInvoice already exists for " +
                "TxId={TxId}. Commit idempotent skip.", message.TransactionId);
            return;
        }

        var kdvRate      = _config.GetValue<decimal>("Payment:DefaultKdvRate", 0.20m);
        var dueDays      = _config.GetValue<int>("Payment:SubscriptionDueDays", 7);

        var plan = (await _providerPlans.GetByIdAsync(message.ContextId, ct))!;

        // ── 1. Create or reuse subscription ──────────────────────────────────
        var subscription = await _providerPlans.GetSubscriptionByTransactionIdAsync(message.TransactionId, ct);
        if (subscription is null)
        {
            var periodStart = message.CapturedAtUtc.Date;
            var periodEnd   = periodStart.AddMonths(1).AddDays(-1);

            subscription = Domain.Entities.Subscription.ProviderPlanSubscriptionEntity.Create(
                providerProfileId:            message.PayerProfileId,
                providerPlanId:               plan.Id,
                paidAmount:                   message.GrossAmount,
                currencyCode:                 message.CurrencyCode,
                periodStart:                  DateTime.SpecifyKind(periodStart, DateTimeKind.Utc),
                periodEnd:                    DateTime.SpecifyKind(periodEnd,   DateTimeKind.Utc),
                autoRenew:                    true,
                paymentTransactionId:         message.TransactionId,
                commissionRateAtSubscription: 0m); // Commission rate is plan-level, not subscription-level

            await _providerPlans.AddSubscriptionAsync(subscription, ct);
            // First SaveChanges: needed to obtain subscription.Id for invoice SourceId.
            await _providerPlans.SaveChangesAsync(ct);

            _logger.LogInformation(
                "ProviderSubscription created. ProfileId={ProfileId} PlanCode={PlanCode} " +
                "TxId={TxId} PeriodEnd={End}",
                message.PayerProfileId, plan.PlanCode, message.TransactionId, periodEnd);
        }

        // ── 2. Build SubscriptionInvoice ─────────────────────────────────────
        var grossAmount   = message.GrossAmount;
        var taxableAmount = Math.Round(grossAmount / (1m + kdvRate), 4, MidpointRounding.AwayFromZero);
        var taxAmount     = Math.Round(grossAmount - taxableAmount, 4, MidpointRounding.AwayFromZero);
        var buyerName     = $"Provider #{message.PayerProfileId}";  // TODO post-MVP: resolve from Identity

        var invoice = InvoiceHeaderEntity.CreateDraft(
            invoiceType:          InvoiceType.SubscriptionInvoice,
            commercialModel:      CommercialModel.SubscriptionBilling,
            sourceType:           InvoiceSourceType.Subscription,
            sourceId:             subscription.Id,
            sellerName:           "Inktavia Marine OS",
            buyerName:            buyerName,
            currency:             message.CurrencyCode,
            subTotalAmount:       taxableAmount,
            discountAmount:       0m,
            taxableAmount:        taxableAmount,
            taxAmount:            taxAmount,
            totalAmount:          grossAmount,
            sellerUserId:         null,
            buyerUserId:          message.PayerProfileId,
            paymentTransactionId: message.TransactionId,
            userSubscriptionId:   subscription.Id,
            dueDateUtc:           DateTime.UtcNow.AddDays(dueDays),
            notes: $"Subscription invoice for plan '{plan.PlanCode}' ({plan.Name}). " +
                   $"Period: {subscription.SubscriptionPeriodStart:yyyy-MM-dd} → " +
                   $"{subscription.SubscriptionPeriodEnd:yyyy-MM-dd}.");

        var line = InvoiceLineEntity.Create(
            invoiceHeaderId:     0,
            lineNumber:          1,
            lineType:            InvoiceLineType.SubscriptionFee,
            description:         $"Inktavia Marine OS — {plan.Name} Plan (Monthly)",
            productCode:         plan.PlanCode,
            serviceCategoryCode: "PROVIDER_SUBSCRIPTION",
            quantity:            1m,
            unitCode:            "MONTH",
            unitPrice:           taxableAmount,
            discountAmount:      0m,
            taxRate:             kdvRate,
            sourceType:          null,
            sourceId:            null);

        invoice.AddLine(line);

        var breakdown = InvoiceTaxBreakdownEntity.Create(
            invoiceHeaderId: 0,
            taxType:         $"KDV{(int)Math.Round(kdvRate * 100)}",
            taxRate:         kdvRate,
            taxableAmount:   taxableAmount);

        invoice.AddTaxBreakdown(breakdown);
        await _invoices.AddAsync(invoice, ct);

        var invoiceNumber = await _numberService.GenerateAsync(InvoiceType.SubscriptionInvoice, ct);
        invoice.Issue(invoiceNumber, issuedByUserId: null);
        _invoices.Update(invoice);

        // Second SaveChanges: saves the issued invoice.
        // WS1: the partial-unique index on (PaymentTransactionId, InvoiceType) is the race backstop — if a
        // concurrent commit copy won the insert, swallow the unique-violation as benign (already processed).
        try
        {
            await _invoices.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (PaymentIdempotency.IsUniqueViolation(ex))
        {
            _logger.LogWarning(
                "ProviderSubscriptionPaymentSucceededConsumer: concurrent commit already issued a " +
                "SubscriptionInvoice for TxId={TxId} (unique-violation swallowed). Idempotent skip.",
                message.TransactionId);
            return;
        }

        _logger.LogInformation(
            "SubscriptionInvoice issued. Number={Number} ProfileId={ProfileId} PlanCode={Code} " +
            "Amount={Amount} {Currency}",
            invoiceNumber, message.PayerProfileId, plan.PlanCode, grossAmount, message.CurrencyCode);

        // ── 3. Publish outbound event ─────────────────────────────────────────
        _ = _publisher.PublishAsync(new SubscriptionPaymentSucceededMessage
        {
            SubscriptionId  = subscription.Id,
            ProfileId       = message.PayerProfileId,
            ProfileType     = "Provider",
            PlanId          = plan.Id,
            PlanCode        = plan.PlanCode,
            PlanName        = plan.Name,
            PaidAmount      = grossAmount,
            CurrencyCode    = message.CurrencyCode,
            GatewayProvider = "Iyzico",
            PeriodStart     = subscription.SubscriptionPeriodStart,
            PeriodEnd       = subscription.SubscriptionPeriodEnd,
            InvoiceId       = invoice.Id,
            InvoiceNumber   = invoiceNumber,
        }, ct).ContinueWith(t =>
        {
            if (t.IsFaulted)
                _logger.LogError(t.Exception,
                    "Failed to publish SubscriptionPaymentSucceededMessage for TxId={TxId}",
                    message.TransactionId);
        }, TaskContinuationOptions.OnlyOnFaulted);
    }

    public override Task ExecuteRollbackMessage(
        PaymentCapturedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogError(
            "ProviderSubscriptionPaymentSucceededConsumer rollback for TxId={TxId}: {Error}",
            message.TransactionId, ex.Message);
        return Task.CompletedTask;
    }
}
