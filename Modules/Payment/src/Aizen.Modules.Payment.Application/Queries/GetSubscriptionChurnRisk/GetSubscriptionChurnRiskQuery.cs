using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.Payment.Application.Queries.GetSubscriptionChurnRisk;

public sealed class GetSubscriptionChurnRiskQuery : AizenQuery<SubscriptionChurnRiskResult>;

public sealed record SubscriptionChurnRiskResult(
    /// <summary>Total PastDue subscriptions (provider + participant) — payment failure risk.</summary>
    int PaymentFailureRiskCount,

    /// <summary>Active subscriptions expiring within 7 days — potential churn without renewal.</summary>
    int ExpiringIn7DaysCount,

    /// <summary>Total at-risk count (union of the above signals).</summary>
    int TotalAtRiskCount
);
