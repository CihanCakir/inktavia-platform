using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Model.Result;
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
    private readonly ILogger<SubscribeProviderPlanCommandHandler>    _logger;

    public SubscribeProviderPlanCommandHandler(
        IAizenUnitOfWork<PaymentDbContext>             unitOfWork,
        IProviderPlanRepository                        plans,
        ILogger<SubscribeProviderPlanCommandHandler>   logger)
    {
        _plans  = plans;
        _logger = logger;
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

        // Commission rate snapshot — 0 for free plans (no commission rule lookup needed)
        const decimal defaultCommissionRate = 0m;

        var subscription = ProviderPlanSubscriptionEntity.Create(
            providerProfileId:           request.ProviderProfileId,
            providerPlanId:              request.ProviderPlanId,
            paidAmount:                  request.PaidAmount,
            currencyCode:                request.CurrencyCode,
            periodStart:                 DateTime.SpecifyKind(request.PeriodStart, DateTimeKind.Utc),
            periodEnd:                   DateTime.SpecifyKind(request.PeriodEnd,   DateTimeKind.Utc),
            autoRenew:                   request.AutoRenew,
            paymentTransactionId:        request.PaymentTransactionId,
            commissionRateAtSubscription: defaultCommissionRate);

        await _plans.AddSubscriptionAsync(subscription, ct);
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
