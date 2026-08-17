using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Model;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Application.Services;
using Aizen.Modules.Payment.Domain.Entities.Subscription;
using Aizen.Modules.Payment.Domain.Entities.Transaction;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Domain.Money;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.SubscribeParticipantForOwner;

/// <summary>
/// BE-MO7 — owner self-subscribe. Guards double-subscribe (existing active → SubscriptionAlreadyActive) and
/// plan-not-found, exactly like the admin <c>SubscribeParticipantPlan</c>. FREE/launch plan (MonthlyPriceTRY == 0) →
/// the subscription is created immediately (mirrors the admin subscribe with PaidAmount 0; no ledger post, no money).
/// PAID plan → a PendingIntent Subscription-context checkout transaction is created and the gateway checkout is
/// initiated (mirrors the P11 boost checkout; non-marketplace, no split, no escrow); the subscription itself is
/// created by the existing <c>ParticipantSubscriptionPaymentSucceededConsumer</c> when the payment captures. The
/// price is always resolved from the plan server-side — never client-supplied. No plan pricing / billing logic
/// changes; this only orchestrates the reused pieces.
/// </summary>
public sealed class SubscribeParticipantForOwnerCommandHandler
    : AizenCommandHandler<SubscribeParticipantForOwnerCommand, SubscribeParticipantForOwnerResult>
{
    private readonly IParticipantPlanRepository    _plans;
    private readonly IPaymentTransactionRepository _transactions;
    private readonly PaymentGatewayResolver        _gatewayResolver;
    private readonly ILogger<SubscribeParticipantForOwnerCommandHandler> _logger;

    public SubscribeParticipantForOwnerCommandHandler(
        IParticipantPlanRepository    plans,
        IPaymentTransactionRepository transactions,
        PaymentGatewayResolver        gatewayResolver,
        ILogger<SubscribeParticipantForOwnerCommandHandler> logger)
    {
        _plans           = plans;
        _transactions    = transactions;
        _gatewayResolver = gatewayResolver;
        _logger          = logger;
    }

    public override async Task<SubscribeParticipantForOwnerResult?> Handle(
        SubscribeParticipantForOwnerCommand request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        // ── Double-subscribe guard (reused rule) ─────────────────────────────────
        var existing = await _plans.GetActiveSubscriptionAsync(request.ParticipantProfileId, now, ct);
        if (existing is not null)
            throw new AizenBusinessException((int)PaymentErrorCode.SubscriptionAlreadyActive);

        var plan = await _plans.GetByIdAsync(request.ParticipantPlanId, ct);
        if (plan is null || !plan.IsActive)
            throw new AizenBusinessException((int)PaymentErrorCode.ParticipantPlanNotFound);

        var currency = "TRY";

        // ── FREE / launch plan → applied immediately (no payment) ────────────────
        if (plan.IsFree)
        {
            var periodStart = DateTime.SpecifyKind(now, DateTimeKind.Utc);
            var periodEnd   = DateTime.SpecifyKind(now.AddMonths(1).AddDays(-1), DateTimeKind.Utc);

            var subscription = ParticipantPlanSubscriptionEntity.Create(
                participantProfileId:          request.ParticipantProfileId,
                participantPlanId:             plan.Id,
                paidAmount:                    0m,
                currencyCode:                  currency,
                periodStart:                   periodStart,
                periodEnd:                     periodEnd,
                autoRenew:                     true,
                paymentTransactionId:          null,
                serviceDiscountAtSubscription: plan.ServiceDiscountRate,
                earnMultiplierAtSubscription:  plan.InkCoinEarnMultiplier);

            await _plans.AddSubscriptionAsync(subscription, ct);
            await _plans.SaveChangesAsync(ct);   // materialise subscription.Id

            _logger.LogInformation(
                "Owner subscribed to FREE plan. Participant={Participant} Plan={PlanCode} Sub={SubId}",
                request.ParticipantProfileId, plan.PlanCode, subscription.Id);

            return new SubscribeParticipantForOwnerResult(
                Mode:                "Immediate",
                ParticipantPlanId:   plan.Id,
                PlanCode:            plan.PlanCode,
                PlanName:            plan.Name,
                Amount:              0m,
                CurrencyCode:        currency,
                SubscriptionId:      subscription.Id,
                TransactionId:       null,
                CheckoutFormContent: null,
                RedirectUrl:         null);
        }

        // ── PAID plan → iyzico-gated checkout (subscription created by the capture consumer) ──
        var price = MoneyMath.Round(plan.MonthlyPriceTRY);
        var idempotencyKey = $"PSUB-{request.ParticipantProfileId}-PLAN-{plan.Id}";

        // Idempotent re-initiate: reuse a still-Pending checkout for the same (participant, plan) rather than
        // creating a second charge.
        var priorTx = await _transactions.GetByIdempotencyKeyAsync(idempotencyKey, ct);
        if (priorTx is not null && priorTx.Status == PaymentTransactionStatus.PendingIntent)
        {
            return new SubscribeParticipantForOwnerResult(
                Mode:                "PaymentPending",
                ParticipantPlanId:   plan.Id,
                PlanCode:            plan.PlanCode,
                PlanName:            plan.Name,
                Amount:              priorTx.GrossAmount,
                CurrencyCode:        priorTx.CurrencyCode,
                SubscriptionId:      null,
                TransactionId:       priorTx.Id,
                CheckoutFormContent: null,   // the form is only returned at first initiation
                RedirectUrl:         null);
        }

        var transactionCode = $"TXN-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..24];
        var tx = PaymentTransactionEntity.Create(
            transactionCode:        transactionCode,
            transactionType:        TransactionType.ParticipantSubscription,
            contextType:            TransactionContextType.Subscription,
            contextId:              plan.Id,                 // consumer discriminates by ContextId = plan id
            contextSubId:           null,
            payerProfileId:         request.ParticipantProfileId,   // the participant pays for their own subscription
            recipientProfileId:     null,                    // non-marketplace → whole amount to the main merchant
            grossAmount:            price,
            commissionAmount:       0m,
            commissionRateSnapshot: 0m,
            vatOnCommission:        0m,
            netPayoutAmount:        0m,
            discountAmount:         0m,
            currencyCode:           currency,
            gatewayProvider:        _gatewayResolver.Resolve().ProviderKey,
            idempotencyKey:         idempotencyKey,
            escrowRequired:         false);
        await _transactions.AddAsync(tx, ct);
        await _transactions.SaveChangesAsync(ct);   // materialise tx.Id

        var gateway = _gatewayResolver.Resolve();
        var init = await gateway.InitiateCheckoutAsync(new CheckoutInitInput
        {
            TransactionId          = tx.Id,
            IdempotencyKey         = idempotencyKey,
            GrossAmount            = price,
            CurrencyCode           = currency,
            PayerProfileId         = request.ParticipantProfileId,
            RecipientProfileId     = null,
            SubMerchantKey         = null,      // non-marketplace: no split
            ProviderNetAmount      = 0m,
            ExpectedRetainedAmount = null,
            Context                = TransactionContext.ForSubscription(plan.Id, plan.Id),
            EscrowRequired         = false,
            Description            = $"Participant subscription — {plan.Name} ({plan.PlanCode})",
        }, ct);

        if (!init.IsSuccess)
            throw new AizenBusinessException((int)PaymentErrorCode.GatewayInitiationFailed);

        tx.AttachGatewayReference(init.GatewayReference);
        _transactions.Update(tx);
        // SaveChanges handled by AizenCommandHandlerDecorator.

        _logger.LogInformation(
            "Owner subscription checkout initiated (PendingIntent). Participant={Participant} Plan={PlanCode} " +
            "Tx={TxId} Price={Price} {Cur} — subscription created on capture.",
            request.ParticipantProfileId, plan.PlanCode, tx.Id, price, currency);

        return new SubscribeParticipantForOwnerResult(
            Mode:                "PaymentPending",
            ParticipantPlanId:   plan.Id,
            PlanCode:            plan.PlanCode,
            PlanName:            plan.Name,
            Amount:              price,
            CurrencyCode:        currency,
            SubscriptionId:      null,
            TransactionId:       tx.Id,
            CheckoutFormContent: init.CheckoutFormContent,
            RedirectUrl:         init.RedirectUrl);
    }
}
