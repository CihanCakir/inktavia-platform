using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.CancelParticipantSubscription;

[DocumentationInfo("Cancel participant subscription command handler",
    "Cancels the active participant subscription. Access to plan benefits is retained until PeriodEnd (MVP — no pro-rata refund).")]
public sealed class CancelParticipantSubscriptionCommandHandler
    : AizenCommandHandler<CancelParticipantSubscriptionCommand, CancelSubscriptionResult>
{
    private readonly IParticipantPlanRepository                              _plans;
    private readonly ILogger<CancelParticipantSubscriptionCommandHandler>    _logger;

    public CancelParticipantSubscriptionCommandHandler(
        IAizenUnitOfWork<PaymentDbContext>                   unitOfWork,
        IParticipantPlanRepository                           plans,
        ILogger<CancelParticipantSubscriptionCommandHandler> logger)
    {
        _plans  = plans;
        _logger = logger;
    }

    public override async Task<CancelSubscriptionResult?> Handle(
        CancelParticipantSubscriptionCommand request, CancellationToken ct)
    {
        var subscription = await _plans.GetActiveSubscriptionAsync(
            request.ParticipantProfileId, DateTime.UtcNow, ct);

        if (subscription is null)
            throw new AizenBusinessException((int)PaymentErrorCode.SubscriptionNotFound);

        subscription.Cancel(request.CancellationReason);
        _plans.UpdateSubscription(subscription);
        // SaveChanges handled by AizenCommandHandlerDecorator.

        _logger.LogInformation(
            "Participant subscription cancelled. ParticipantProfileId={ParticipantId} SubscriptionId={SubId}",
            request.ParticipantProfileId, subscription.Id);

        return new CancelSubscriptionResult(
            SubscriptionId: subscription.Id,
            Status:         subscription.Status.ToString(),
            CancelledAt:    subscription.CancelledAt ?? DateTime.UtcNow);
    }
}
