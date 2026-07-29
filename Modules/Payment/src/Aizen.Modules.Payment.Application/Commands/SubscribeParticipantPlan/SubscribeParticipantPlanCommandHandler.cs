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

namespace Aizen.Modules.Payment.Application.Commands.SubscribeParticipantPlan;

[DocumentationInfo("Subscribe participant plan command handler",
    "Creates a new ParticipantPlanSubscriptionEntity. Snapshots discount rate and earn multiplier from the plan at subscription time.")]
public sealed class SubscribeParticipantPlanCommandHandler
    : AizenCommandHandler<SubscribeParticipantPlanCommand, SubscribeParticipantPlanResult>
{
    private readonly IParticipantPlanRepository                         _plans;
    private readonly FinancialLedgerPostingService                      _ledgerPosting;
    private readonly ILogger<SubscribeParticipantPlanCommandHandler>    _logger;

    public SubscribeParticipantPlanCommandHandler(
        IAizenUnitOfWork<PaymentDbContext>              unitOfWork,
        IParticipantPlanRepository                      plans,
        FinancialLedgerPostingService                   ledgerPosting,
        ILogger<SubscribeParticipantPlanCommandHandler> logger)
    {
        _plans         = plans;
        _ledgerPosting = ledgerPosting;
        _logger        = logger;
    }

    public override async Task<SubscribeParticipantPlanResult?> Handle(
        SubscribeParticipantPlanCommand request, CancellationToken ct)
    {
        var existing = await _plans.GetActiveSubscriptionAsync(
            request.ParticipantProfileId, DateTime.UtcNow, ct);

        if (existing is not null)
            throw new AizenBusinessException((int)PaymentErrorCode.SubscriptionAlreadyActive);

        var plan = await _plans.GetByIdAsync(request.ParticipantPlanId, ct);
        if (plan is null || !plan.IsActive)
            throw new AizenBusinessException((int)PaymentErrorCode.ParticipantPlanNotFound);

        var subscription = ParticipantPlanSubscriptionEntity.Create(
            participantProfileId:              request.ParticipantProfileId,
            participantPlanId:                 request.ParticipantPlanId,
            paidAmount:                        request.PaidAmount,
            currencyCode:                      request.CurrencyCode,
            periodStart:                       DateTime.SpecifyKind(request.PeriodStart, DateTimeKind.Utc),
            periodEnd:                         DateTime.SpecifyKind(request.PeriodEnd,   DateTimeKind.Utc),
            autoRenew:                         request.AutoRenew,
            paymentTransactionId:              request.PaymentTransactionId,
            serviceDiscountAtSubscription:     plan.ServiceDiscountRate,
            earnMultiplierAtSubscription:      plan.InkCoinEarnMultiplier);

        await _plans.AddSubscriptionAsync(subscription, ct);

        // ── BE-P12: SubscriptionRevenue + the §19.17 CustomerPlanRevenue split (derived from the snapshotted paid amount). ──
        if (request.PaidAmount > 0m)
        {
            await _plans.SaveChangesAsync(ct);   // materialise subscription.Id for the ledger SourceRef
            await _ledgerPosting.PostSubscriptionAsync(
                subscription.Id, request.PaidAmount, request.CurrencyCode, isProvider: false,
                profileId: request.ParticipantProfileId, transactionId: request.PaymentTransactionId,
                occurredAtUtc: DateTime.UtcNow, ct);
        }
        // SaveChanges handled by AizenCommandHandlerDecorator.

        _logger.LogInformation(
            "Participant subscription created. ParticipantProfileId={ParticipantId} PlanId={PlanId} PeriodEnd={End}",
            request.ParticipantProfileId, request.ParticipantPlanId, request.PeriodEnd);

        return new SubscribeParticipantPlanResult(
            SubscriptionId:      subscription.Id,
            PlanCode:            plan.PlanCode,
            PlanName:            plan.Name,
            PaidAmount:          subscription.PaidAmount,
            CurrencyCode:        subscription.CurrencyCode,
            PeriodStart:         subscription.SubscriptionPeriodStart,
            PeriodEnd:           subscription.SubscriptionPeriodEnd,
            AutoRenew:           subscription.AutoRenew,
            ServiceDiscountRate: plan.ServiceDiscountRate,
            InkCoinEarnMultiplier: plan.InkCoinEarnMultiplier);
    }
}
