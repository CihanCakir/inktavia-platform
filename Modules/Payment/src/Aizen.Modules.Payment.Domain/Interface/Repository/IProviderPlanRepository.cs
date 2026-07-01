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

    Task AddSubscriptionAsync(ProviderPlanSubscriptionEntity entity, CancellationToken ct = default);
    void UpdateSubscription(ProviderPlanSubscriptionEntity entity);

    Task AddAsync(ProviderPlanEntity entity, CancellationToken ct = default);
    void Update(ProviderPlanEntity entity);
    Task SaveChangesAsync(CancellationToken ct = default);
}
