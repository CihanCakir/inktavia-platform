using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Application.Services;
using Aizen.Modules.Payment.Domain.Entities.Subscription;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.SubscribeProviderPlan;

[DocumentationInfo("Subscribe provider plan command handler",
    "Creates a new ProviderPlanSubscriptionEntity for a billing period. Rejects if an active subscription already exists.")]
public sealed class SubscribeProviderPlanCommandHandler
    : AizenCommandHandler<SubscribeProviderPlanCommand, SubscribeProviderPlanResult>
{
    private readonly IProviderPlanRepository                         _plans;
    private readonly IProviderPlanPriceRepository                    _planPrices;
    private readonly FinancialLedgerPostingService                   _ledgerPosting;
    private readonly ILogger<SubscribeProviderPlanCommandHandler>    _logger;

    public SubscribeProviderPlanCommandHandler(
        IAizenUnitOfWork<PaymentDbContext>             unitOfWork,
        IProviderPlanRepository                        plans,
        IProviderPlanPriceRepository                   planPrices,
        FinancialLedgerPostingService                  ledgerPosting,
        ILogger<SubscribeProviderPlanCommandHandler>   logger)
    {
        _plans         = plans;
        _planPrices    = planPrices;
        _ledgerPosting = ledgerPosting;
        _logger        = logger;
    }

    public override async Task<SubscribeProviderPlanResult?> Handle(
        SubscribeProviderPlanCommand request, CancellationToken ct)
    {
        // Idempotency / conflict guard
        var existing = await _plans.GetActiveSubscriptionAsync(
            request.ProviderProfileId, DateTime.UtcNow, ct);

        if (existing is not null)
            throw new AizenBusinessException((int)PaymentErrorCode.SubscriptionAlreadyActive);

        var plan = await _plans.GetByIdAsync(request.ProviderPlanId, ct);
        if (plan is null || !plan.IsActive)
            throw new AizenBusinessException((int)PaymentErrorCode.ProviderPlanNotFound);

        // ── BE-P4: resolve the authoritative price from ProviderPlanPrice and SNAPSHOT it. ──
        // Never trust ProviderPlan.MonthlyPriceTRY, and don't blindly snapshot request.PaidAmount.
        var resolvedPrice = await _planPrices.ResolveAsync(
            request.ProviderPlanId, request.CurrencyCode, request.BillingPeriod, DateTime.UtcNow, ct);

        decimal paidAmount;
        if (resolvedPrice is not null)
        {
            paidAmount = resolvedPrice.PriceAmount;   // authoritative snapshot
            if (request.PaidAmount != paidAmount)
                _logger.LogWarning(
                    "Subscribe PaidAmount mismatch ignored: request={Requested} resolved={Resolved} (PlanId={PlanId}). " +
                    "Snapshotting the resolved ProviderPlanPrice.",
                    request.PaidAmount, paidAmount, request.ProviderPlanId);
        }
        else
        {
            // No price row for this (plan, currency, period) — fall back to the request (legacy) with a warning.
            paidAmount = request.PaidAmount;
            _logger.LogWarning(
                "No ProviderPlanPrice resolved for PlanId={PlanId} {Currency} {Period}; falling back to request.PaidAmount={Amount}.",
                request.ProviderPlanId, request.CurrencyCode, request.BillingPeriod, request.PaidAmount);
        }

        // Commission rate snapshot — 0 for free plans (no commission rule lookup needed)
        const decimal defaultCommissionRate = 0m;

        var subscription = ProviderPlanSubscriptionEntity.Create(
            providerProfileId:           request.ProviderProfileId,
            providerPlanId:              request.ProviderPlanId,
            paidAmount:                  paidAmount,
            currencyCode:                request.CurrencyCode,
            periodStart:                 DateTime.SpecifyKind(request.PeriodStart, DateTimeKind.Utc),
            periodEnd:                   DateTime.SpecifyKind(request.PeriodEnd,   DateTimeKind.Utc),
            autoRenew:                   request.AutoRenew,
            paymentTransactionId:        request.PaymentTransactionId,
            commissionRateAtSubscription: defaultCommissionRate);

        await _plans.AddSubscriptionAsync(subscription, ct);

        // ── BE-P12: SubscriptionRevenue + the §19.17 ProviderPlanRevenue split (derived from the snapshotted paid amount). ──
        if (paidAmount > 0m)
        {
            await _plans.SaveChangesAsync(ct);   // materialise subscription.Id for the ledger SourceRef
            await _ledgerPosting.PostSubscriptionAsync(
                subscription.Id, paidAmount, request.CurrencyCode, isProvider: true,
                profileId: request.ProviderProfileId, transactionId: request.PaymentTransactionId,
                occurredAtUtc: DateTime.UtcNow, ct);
        }
        // SaveChanges handled by AizenCommandHandlerDecorator — do NOT call here.

        _logger.LogInformation(
            "Provider subscription created. ProviderProfileId={ProviderId} PlanId={PlanId} PeriodEnd={End}",
            request.ProviderProfileId, request.ProviderPlanId, request.PeriodEnd);

        return new SubscribeProviderPlanResult(
            SubscriptionId: subscription.Id,
            PlanCode:       plan.PlanCode,
            PlanName:       plan.Name,
            PaidAmount:     subscription.PaidAmount,
            CurrencyCode:   subscription.CurrencyCode,
            PeriodStart:    subscription.SubscriptionPeriodStart,
            PeriodEnd:      subscription.SubscriptionPeriodEnd,
            AutoRenew:      subscription.AutoRenew);
    }
}
