using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.CancelProviderSubscription;

[DocumentationInfo("Cancel provider subscription command handler",
    "Cancels the active provider subscription. Subscription remains valid until PeriodEnd (MVP — no pro-rata refund).")]
public sealed class CancelProviderSubscriptionCommandHandler
    : AizenCommandHandler<CancelProviderSubscriptionCommand, CancelSubscriptionResult>
{
    private readonly IProviderPlanRepository                              _plans;
    private readonly ILogger<CancelProviderSubscriptionCommandHandler>    _logger;

    public CancelProviderSubscriptionCommandHandler(
        IAizenUnitOfWork<PaymentDbContext>                  unitOfWork,
        IProviderPlanRepository                             plans,
        ILogger<CancelProviderSubscriptionCommandHandler>   logger)
    {
        _plans  = plans;
        _logger = logger;
    }

    public override async Task<CancelSubscriptionResult?> Handle(
        CancelProviderSubscriptionCommand request, CancellationToken ct)
    {
        var subscription = await _plans.GetActiveSubscriptionAsync(
            request.ProviderProfileId, DateTime.UtcNow, ct);

        if (subscription is null)
            throw new AizenBusinessException((int)PaymentErrorCode.SubscriptionNotFound);

        subscription.Cancel(request.CancellationReason);
        _plans.UpdateSubscription(subscription);
        // SaveChanges handled by AizenCommandHandlerDecorator.

        _logger.LogInformation(
            "Provider subscription cancelled. ProviderProfileId={ProviderId} SubscriptionId={SubId}",
            request.ProviderProfileId, subscription.Id);

        return new CancelSubscriptionResult(
            SubscriptionId: subscription.Id,
            Status:         subscription.Status.ToString(),
            CancelledAt:    subscription.CancelledAt ?? DateTime.UtcNow);
    }
}
