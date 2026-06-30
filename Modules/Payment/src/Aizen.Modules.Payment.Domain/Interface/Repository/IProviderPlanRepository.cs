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
    Task AddSubscriptionAsync(ProviderPlanSubscriptionEntity entity, CancellationToken ct = default);
    void UpdateSubscription(ProviderPlanSubscriptionEntity entity);

    Task AddAsync(ProviderPlanEntity entity, CancellationToken ct = default);
    void Update(ProviderPlanEntity entity);
    Task SaveChangesAsync(CancellationToken ct = default);
}
