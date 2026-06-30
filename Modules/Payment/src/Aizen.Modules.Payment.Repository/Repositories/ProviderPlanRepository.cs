using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Plan;
using Aizen.Modules.Payment.Domain.Entities.Subscription;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Payment.Repository.Repositories;

public sealed class ProviderPlanRepository : IProviderPlanRepository
{
    private readonly PaymentDbContext _db;
    public ProviderPlanRepository(PaymentDbContext db) => _db = db;

    public Task<List<ProviderPlanEntity>> GetAllActiveAsync(CancellationToken ct)
        => _db.ProviderPlans.Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToListAsync(ct);

    public Task<ProviderPlanEntity?> GetByIdAsync(long id, CancellationToken ct)
        => _db.ProviderPlans.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<ProviderPlanEntity?> GetByCodeAsync(string planCode, CancellationToken ct)
        => _db.ProviderPlans.FirstOrDefaultAsync(x => x.PlanCode == planCode, ct);

    public Task<bool> ExistsByCodeAsync(string planCode, CancellationToken ct)
        => _db.ProviderPlans.AnyAsync(x => x.PlanCode == planCode, ct);

    public Task<ProviderPlanSubscriptionEntity?> GetActiveSubscriptionAsync(long providerProfileId, DateTime atUtc, CancellationToken ct)
        => _db.ProviderSubscriptions.FirstOrDefaultAsync(x =>
            x.ProviderProfileId == providerProfileId &&
            x.Status == SubscriptionStatus.Active &&
            x.SubscriptionPeriodStart <= atUtc &&
            x.SubscriptionPeriodEnd >= atUtc, ct);

    public Task AddSubscriptionAsync(ProviderPlanSubscriptionEntity entity, CancellationToken ct)
        => _db.ProviderSubscriptions.AddAsync(entity, ct).AsTask();

    public void UpdateSubscription(ProviderPlanSubscriptionEntity entity)
        => _db.ProviderSubscriptions.Update(entity);

    public Task AddAsync(ProviderPlanEntity entity, CancellationToken ct)
        => _db.ProviderPlans.AddAsync(entity, ct).AsTask();

    public void Update(ProviderPlanEntity entity) => _db.ProviderPlans.Update(entity);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
