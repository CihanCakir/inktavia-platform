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
/// is a ParticipantPlanEntity. Activates the participant's subscription and auto-issues a
/// SubscriptionInvoice.
///
/// ── Payment gateway paths for Participant subscriptions ───────────────────────
///
///  PATH A — Web/Card via Iyzico (currently implemented):
///    Participant selects plan on web → Iyzico checkout → webhook → PaymentCapturedMessage
///    → this consumer handles it.
///
///  PATH B — iOS via Apple App Store (Phase 2C):
///    Participant selects plan in iOS app → StoreKit completes IAP → app sends receipt to BFF
///    → BFF calls POST /api/subscriptions/verify/apple (new endpoint in Phase 2C)
///    → IAppleAppStoreGatewayClient.ValidateReceiptAsync(receipt)
///    → on success: directly creates ParticipantPlanSubscription (bypasses this consumer)
///    → publishes SubscriptionPaymentSucceededMessage(GatewayProvider="AppleAppStore")
///
///  PATH C — Android via Google Play Billing (Phase 2C):
///    Similar to PATH B but uses IGooglePlayGatewayClient.ValidatePurchaseTokenAsync(...)
///    → POST /api/subscriptions/verify/google (new endpoint in Phase 2C)
///    → publishes SubscriptionPaymentSucceededMessage(GatewayProvider="GooglePlayStore")
///
///  Phase 2C implementation: register IAppleAppStoreGatewayClient and IGooglePlayGatewayClient
///  in DependencyInjection.cs and create VerifyAppleSubscriptionCommand and
///  VerifyGooglePlaySubscriptionCommand.
///
/// ── Context mapping ───────────────────────────────────────────────────────────
///  ContextId       = ParticipantPlanEntity.Id
///  PayerProfileId  = ParticipantProfileId (participant pays for their subscription)
///  GrossAmount     = plan monthly price paid
///
/// ── Discrimination strategy ───────────────────────────────────────────────────
///  ExecutePrepareMessage checks if ContextId exists in ParticipantPlanEntity.
///  If ProviderPlanEntity exists for the same ID, ProviderSubscriptionPaymentSucceededConsumer
///  handles it instead.
///
/// ── Idempotency ───────────────────────────────────────────────────────────────
///  Prepare checks: no existing invoice for this TransactionId.
///  Commit: if subscription was already created on a prior attempt, it is reused.
///
/// Configuration:
///   Payment:DefaultKdvRate       (default 0.20)
///   Payment:SubscriptionDueDays  (default 7)
/// </summary>
public sealed class ParticipantSubscriptionPaymentSucceededConsumer
    : AizenBaseMessageConsumer<PaymentCapturedMessage>
{
    private readonly IParticipantPlanRepository                                  _participantPlans;
    private readonly IInvoiceRepository                                          _invoices;
    private readonly InvoiceNumberService                                        _numberService;
    private readonly IAizenMessagePublisher                                      _publisher;
    private readonly IConfiguration                                              _config;
    private readonly ILogger<ParticipantSubscriptionPaymentSucceededConsumer>    _logger;

    public ParticipantSubscriptionPaymentSucceededConsumer(IServiceProvider sp) : base(sp)
    {
        _participantPlans = sp.GetRequiredService<IParticipantPlanRepository>();
        _invoices         = sp.GetRequiredService<IInvoiceRepository>();
        _numberService    = sp.GetRequiredService<InvoiceNumberService>();
        _publisher        = sp.GetRequiredService<IAizenMessagePublisher>();
        _config           = sp.GetRequiredService<IConfiguration>();
        _logger           = sp.GetRequiredService<ILogger<ParticipantSubscriptionPaymentSucceededConsumer>>();
    }

    public override async Task<bool> ExecutePrepareMessage(
        PaymentCapturedMessage message, CancellationToken ct)
    {
        // Only handle subscription payments
        if (message.ContextType != TransactionContextType.Subscription)
            return false;

        // Discriminate: is ContextId a ParticipantPlan?
        var plan = await _participantPlans.GetByIdAsync(message.ContextId, ct);
        if (plan is null)
        {
            // Not a participant plan — ProviderSubscriptionPaymentSucceededConsumer will handle this.
            return false;
        }

        // Idempotency: invoice already issued for this transaction?
        var existingInvoice = await _invoices.GetByTransactionIdAsync(message.TransactionId, ct);
        if (existingInvoice is not null)
        {
            _logger.LogInformation(
                "ParticipantSubscriptionPaymentSucceededConsumer: SubscriptionInvoice already exists " +
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
        // Re-check the natural key so a duplicate two-phase commit copy returns without issuing a second
        // SubscriptionInvoice / invoice number / outbound event. See the provider consumer for the rationale.
        if (await _invoices.ExistsByTransactionIdAndTypeAsync(
                message.TransactionId, InvoiceType.SubscriptionInvoice, ct))
        {
            _logger.LogInformation(
                "ParticipantSubscriptionPaymentSucceededConsumer: SubscriptionInvoice already exists for " +
                "TxId={TxId}. Commit idempotent skip.", message.TransactionId);
            return;
        }

        var kdvRate  = _config.GetValue<decimal>("Payment:DefaultKdvRate", 0.20m);
        var dueDays  = _config.GetValue<int>("Payment:SubscriptionDueDays", 7);

        var plan = (await _participantPlans.GetByIdAsync(message.ContextId, ct))!;

        // ── 1. Create or reuse subscription ──────────────────────────────────
        var subscription = await _participantPlans.GetSubscriptionByTransactionIdAsync(message.TransactionId, ct);
        if (subscription is null)
        {
            var periodStart = message.CapturedAtUtc.Date;
            var periodEnd   = periodStart.AddMonths(1).AddDays(-1);

            subscription = Domain.Entities.Subscription.ParticipantPlanSubscriptionEntity.Create(
                participantProfileId:           message.PayerProfileId,
                participantPlanId:              plan.Id,
                paidAmount:                     message.GrossAmount,
                currencyCode:                   message.CurrencyCode,
                periodStart:                    DateTime.SpecifyKind(periodStart, DateTimeKind.Utc),
                periodEnd:                      DateTime.SpecifyKind(periodEnd,   DateTimeKind.Utc),
                autoRenew:                      true,
                paymentTransactionId:           message.TransactionId,
                serviceDiscountAtSubscription:  plan.ServiceDiscountRate,
                earnMultiplierAtSubscription:   plan.InkCoinEarnMultiplier);

            await _participantPlans.AddSubscriptionAsync(subscription, ct);
            // First SaveChanges: needed to obtain subscription.Id for invoice SourceId.
            await _participantPlans.SaveChangesAsync(ct);

            _logger.LogInformation(
                "ParticipantSubscription created. ProfileId={ProfileId} PlanCode={PlanCode} " +
                "TxId={TxId} PeriodEnd={End}",
                message.PayerProfileId, plan.PlanCode, message.TransactionId, periodEnd);
        }

        // ── 2. Build SubscriptionInvoice (B2C) ───────────────────────────────
        var grossAmount   = message.GrossAmount;
        var taxableAmount = Math.Round(grossAmount / (1m + kdvRate), 4, MidpointRounding.AwayFromZero);
        var taxAmount     = Math.Round(grossAmount - taxableAmount, 4, MidpointRounding.AwayFromZero);
        var buyerName     = $"Participant #{message.PayerProfileId}"; // TODO post-MVP: resolve from Identity

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
                   $"{subscription.SubscriptionPeriodEnd:yyyy-MM-dd}. " +
                   $"Gateway: Iyzico (card). " +
                   $"Phase 2C: Apple App Store / Google Play paths will generate their own documents.");

        var line = InvoiceLineEntity.Create(
            invoiceHeaderId:     0,
            lineNumber:          1,
            lineType:            InvoiceLineType.SubscriptionFee,
            description:         $"Inktavia Marine OS — {plan.Name} Plan (Monthly)",
            productCode:         plan.PlanCode,
            serviceCategoryCode: "PARTICIPANT_SUBSCRIPTION",
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
        // WS1: partial-unique index on (PaymentTransactionId, InvoiceType) is the race backstop.
        try
        {
            await _invoices.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (PaymentIdempotency.IsUniqueViolation(ex))
        {
            _logger.LogWarning(
                "ParticipantSubscriptionPaymentSucceededConsumer: concurrent commit already issued a " +
                "SubscriptionInvoice for TxId={TxId} (unique-violation swallowed). Idempotent skip.",
                message.TransactionId);
            return;
        }

        _logger.LogInformation(
            "ParticipantSubscriptionInvoice issued. Number={Number} ProfileId={ProfileId} " +
            "PlanCode={Code} Amount={Amount} {Currency}",
            invoiceNumber, message.PayerProfileId, plan.PlanCode, grossAmount, message.CurrencyCode);

        // ── 3. Publish outbound event ─────────────────────────────────────────
        _ = _publisher.PublishAsync(new SubscriptionPaymentSucceededMessage
        {
            SubscriptionId  = subscription.Id,
            ProfileId       = message.PayerProfileId,
            ProfileType     = "Participant",
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
                    "Failed to publish SubscriptionPaymentSucceededMessage(Participant) for TxId={TxId}",
                    message.TransactionId);
        }, TaskContinuationOptions.OnlyOnFaulted);
    }

    public override Task ExecuteRollbackMessage(
        PaymentCapturedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogError(
            "ParticipantSubscriptionPaymentSucceededConsumer rollback for TxId={TxId}: {Error}",
            message.TransactionId, ex.Message);
        return Task.CompletedTask;
    }
}
