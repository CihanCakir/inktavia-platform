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
    Task AddSubscriptionAsync(ParticipantPlanSubscriptionEntity entity, CancellationToken ct = default);
    void UpdateSubscription(ParticipantPlanSubscriptionEntity entity);

    Task AddAsync(ParticipantPlanEntity entity, CancellationToken ct = default);
    void Update(ParticipantPlanEntity entity);
    Task SaveChangesAsync(CancellationToken ct = default);
}
