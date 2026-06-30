using Aizen.Modules.Payment.Domain.Entities.Plan;
using Aizen.Modules.Payment.Domain.Entities.Subscription;

namespace Aizen.Modules.Payment.Domain.Interface.Repository;

public interface IParticipantPlanRepository
{
    Task<List<ParticipantPlanEntity>>  GetAllActiveAsync(CancellationToken ct = default);
    Task<ParticipantPlanEntity?>       GetByIdAsync(long id, CancellationToken ct = default);
    Task<ParticipantPlanEntity?>       GetByCodeAsync(string planCode, CancellationToken ct = default);
    Task<bool>                         ExistsByCodeAsync(string planCode, CancellationToken ct = default);

    // Subscriptions
    Task<ParticipantPlanSubscriptionEntity?> GetActiveSubscriptionAsync(long participantProfileId, DateTime atUtc, CancellationToken ct = default);

    /// <summary>
    /// Returns a subscription linked to a specific payment transaction.
    /// Used by ParticipantSubscriptionPaymentSucceededConsumer for idempotent retry handling.
    /// </summary>
    Task<ParticipantPlanSubscriptionEntity?> GetSubscriptionByTransactionIdAsync(long transactionId, CancellationToken ct = default);

    /// <summary>
    /// Returns Active subscriptions whose SubscriptionPeriodEnd has already passed.
    /// Used by SubscriptionRenewalJob to transition them to PastDue or Expired.
    /// </summary>
    Task<List<ParticipantPlanSubscriptionEntity>> GetExpiredActiveSubscriptionsAsync(
        int batchSize, CancellationToken ct = default);

    /// <summary>
    /// Returns PastDue subscriptions for monitoring and re-notification.
    /// </summary>
    Task<List<ParticipantPlanSubscriptionEntity>> GetPastDueSubscriptionsAsync(
        int batchSize, CancellationToken ct = default);

    Task AddSubscriptionAsync(ParticipantPlanSubscriptionEntity entity, CancellationToken ct = default);
    void UpdateSubscription(ParticipantPlanSubscriptionEntity entity);

    Task AddAsync(ParticipantPlanEntity entity, CancellationToken ct = default);
    void Update(ParticipantPlanEntity entity);
    Task SaveChangesAsync(CancellationToken ct = default);
}
