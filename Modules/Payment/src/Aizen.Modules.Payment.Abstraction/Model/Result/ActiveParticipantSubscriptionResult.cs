using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Abstraction.Model.Result;

public sealed record ActiveParticipantSubscriptionResult(
    long               SubscriptionId,
    long               ParticipantPlanId,
    string             PlanCode,
    SubscriptionStatus Status,
    decimal            PaidAmount,
    string             CurrencyCode,
    DateTime           PeriodStart,
    DateTime           PeriodEnd,
    bool               AutoRenew,
    decimal            ServiceDiscountAtSubscription,
    decimal            EarnMultiplierAtSubscription
);
