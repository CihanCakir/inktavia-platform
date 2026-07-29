using Aizen.Modules.Payment.Domain.Entities.Plan;
using Aizen.Modules.Payment.Domain.Entities.Subscription;

namespace Aizen.Modules.Payment.Domain.Interface.Repository;

public interface IProviderPlanRepository
{
    Task<List<ProviderPlanEntity>>  GetAllActiveAsync(CancellationToken ct = default);
    Task<ProviderPlanEntity?>       GetByIdAsync(long id, CancellationToken ct = default);
    Task<ProviderPlanEntity?>       GetByCodeAsync(string planCode, CancellationToken ct = default);
    Task<bool>                      ExistsByCodeAsync(string planCode, CancellationToken ct = default);

    // Subscriptions
    Task<ProviderPlanSubscriptionEntity?> GetActiveSubscriptionAsync(long providerProfileId, DateTime atUtc, CancellationToken ct = default);

    /// <summary>
    /// Returns a subscription linked to a specific payment transaction.
    /// Used by ProviderSubscriptionPaymentSucceededConsumer for idempotent retry handling.
    /// </summary>
    Task<ProviderPlanSubscriptionEntity?> GetSubscriptionByTransactionIdAsync(long transactionId, CancellationToken ct = default);

    /// <summary>
    /// Returns Active subscriptions whose SubscriptionPeriodEnd has already passed.
    /// Used by SubscriptionRenewalJob to transition them to PastDue or Expired.
    /// </summary>
    Task<List<ProviderPlanSubscriptionEntity>> GetExpiredActiveSubscriptionsAsync(
        int batchSize, CancellationToken ct = default);

    /// <summary>
    /// Returns PastDue subscriptions for monitoring and re-notification.
    /// Used by SubscriptionRenewalJob to republish SubscriptionPaymentFailedMessage.
    /// </summary>
    Task<List<ProviderPlanSubscriptionEntity>> GetPastDueSubscriptionsAsync(
        int batchSize, CancellationToken ct = default);

    /// <summary>Returns the count of Active provider subscriptions still within their period.</summary>
    Task<int> CountActiveSubscriptionsAsync(DateTime utcNow, CancellationToken ct = default);

    /// <summary>Returns the count of Active subscriptions ending within <paramref name="withinDays"/> days.</summary>
    Task<int> CountExpiringSoonAsync(DateTime utcNow, int withinDays, CancellationToken ct = default);

    /// <summary>Returns the count of PastDue subscriptions created within the given month range.</summary>
    Task<int> CountPastDueThisMonthAsync(DateTime monthStart, DateTime monthEnd, CancellationToken ct = default);

    /// <summary>Returns total PaidAmount for Active subscriptions (MRR approximation).</summary>
    Task<decimal> SumActiveMrrAsync(DateTime utcNow, CancellationToken ct = default);

    /// <summary>Returns total count of PastDue provider subscriptions (all time, for churn risk signal).</summary>
    Task<int> CountTotalPastDueAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns monthly PaidAmount totals for provider subscriptions created within the given range.
    /// Each tuple: (Year, Month, TotalPaid). Used for MRR trend calculation.
    /// </summary>
    Task<List<(int Year, int Month, decimal Total)>> GetMonthlyPaidAmountAsync(
        DateTime fromUtc, CancellationToken ct = default);

    /// <summary>
    /// Returns all provider subscriptions for admin oversight list, optionally filtered by status.
    /// Ordered by CreateDate descending. Used by GetAdminSubscriptionListQueryHandler.
    /// </summary>
    Task<List<ProviderPlanSubscriptionEntity>> GetAllSubscriptionsForAdminAsync(
        string? status, CancellationToken ct = default);

    /// <summary>
    /// Returns Active subscriptions whose SubscriptionPeriodEnd (next renewal) falls in [fromUtc, toUtc).
    /// Used by the BE-P4 ≥14-day upcoming-price-change query (feeds Notification N1).
    /// </summary>
    Task<List<ProviderPlanSubscriptionEntity>> GetActiveSubscriptionsRenewingBetweenAsync(
        DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);

    Task AddSubscriptionAsync(ProviderPlanSubscriptionEntity entity, CancellationToken ct = default);
    void UpdateSubscription(ProviderPlanSubscriptionEntity entity);

    Task AddAsync(ProviderPlanEntity entity, CancellationToken ct = default);
    void Update(ProviderPlanEntity entity);
    Task SaveChangesAsync(CancellationToken ct = default);
}
