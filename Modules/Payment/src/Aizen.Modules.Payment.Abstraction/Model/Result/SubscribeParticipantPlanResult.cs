namespace Aizen.Modules.Payment.Abstraction.Model.Result;

public sealed record SubscribeParticipantPlanResult(
    long    SubscriptionId,
    string  PlanCode,
    string  PlanName,
    decimal PaidAmount,
    string  CurrencyCode,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    bool    AutoRenew,
    decimal ServiceDiscountRate,
    decimal InkCoinEarnMultiplier
);
