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

    public Task<List<ProviderPlanEntity>> GetAllAsync(CancellationToken ct)
        => _db.ProviderPlans.OrderBy(x => x.SortOrder).ToListAsync(ct);

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

    public Task<ProviderPlanSubscriptionEntity?> GetSubscriptionByTransactionIdAsync(long transactionId, CancellationToken ct)
        => _db.ProviderSubscriptions.FirstOrDefaultAsync(
            x => x.PaymentTransactionId == transactionId, ct);

    public Task<List<ProviderPlanSubscriptionEntity>> GetExpiredActiveSubscriptionsAsync(
        int batchSize, CancellationToken ct)
        => _db.ProviderSubscriptions
            .Where(x => x.Status == SubscriptionStatus.Active
                     && x.SubscriptionPeriodEnd < DateTime.UtcNow)
            .OrderBy(x => x.SubscriptionPeriodEnd)
            .Take(batchSize)
            .ToListAsync(ct);

    public Task<List<ProviderPlanSubscriptionEntity>> GetPastDueSubscriptionsAsync(
        int batchSize, CancellationToken ct)
        => _db.ProviderSubscriptions
            .Where(x => x.Status == SubscriptionStatus.PastDue)
            .OrderBy(x => x.CreateDate)
            .Take(batchSize)
            .ToListAsync(ct);

    public Task<int> CountActiveSubscriptionsAsync(DateTime utcNow, CancellationToken ct)
        => _db.ProviderSubscriptions.CountAsync(x =>
            x.Status == SubscriptionStatus.Active &&
            x.SubscriptionPeriodStart <= utcNow &&
            x.SubscriptionPeriodEnd   >= utcNow, ct);

    public Task<int> CountExpiringSoonAsync(DateTime utcNow, int withinDays, CancellationToken ct)
    {
        var threshold = utcNow.AddDays(withinDays);
        return _db.ProviderSubscriptions.CountAsync(x =>
            x.Status == SubscriptionStatus.Active &&
            x.SubscriptionPeriodEnd >= utcNow &&
            x.SubscriptionPeriodEnd <= threshold, ct);
    }

    public Task<int> CountPastDueThisMonthAsync(DateTime monthStart, DateTime monthEnd, CancellationToken ct)
        => _db.ProviderSubscriptions.CountAsync(x =>
            x.Status == SubscriptionStatus.PastDue &&
            x.CreateDate >= monthStart &&
            x.CreateDate <  monthEnd, ct);

    public async Task<decimal> SumActiveMrrAsync(DateTime utcNow, CancellationToken ct)
    {
        var amounts = await _db.ProviderSubscriptions
            .Where(x => x.Status == SubscriptionStatus.Active &&
                        x.SubscriptionPeriodStart <= utcNow &&
                        x.SubscriptionPeriodEnd   >= utcNow)
            .Select(x => x.PaidAmount)
            .ToListAsync(ct);
        return amounts.Sum();
    }

    public Task<int> CountTotalPastDueAsync(CancellationToken ct)
        => _db.ProviderSubscriptions.CountAsync(x => x.Status == SubscriptionStatus.PastDue, ct);

    public async Task<List<(int Year, int Month, decimal Total)>> GetMonthlyPaidAmountAsync(
        DateTime fromUtc, CancellationToken ct)
    {
        var raw = await _db.ProviderSubscriptions
            .Where(x => x.CreateDate >= fromUtc)
            .Select(x => new { x.CreateDate, x.PaidAmount })
            .ToListAsync(ct);

        return raw
            .Where(x => x.CreateDate.HasValue)
            .GroupBy(x => new { x.CreateDate!.Value.Year, x.CreateDate!.Value.Month })
            .Select(g => (g.Key.Year, g.Key.Month, g.Sum(x => x.PaidAmount)))
            .ToList();
    }

    public async Task<List<ProviderPlanSubscriptionEntity>> GetAllSubscriptionsForAdminAsync(
        string? status, CancellationToken ct)
    {
        var query = _db.ProviderSubscriptions.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<SubscriptionStatus>(status, ignoreCase: true, out var parsedStatus))
        {
            query = query.Where(x => x.Status == parsedStatus);
        }
        return await query.OrderByDescending(x => x.CreateDate).ToListAsync(ct);
    }

    public Task<List<ProviderPlanSubscriptionEntity>> GetActiveSubscriptionsRenewingBetweenAsync(
        DateTime fromUtc, DateTime toUtc, CancellationToken ct)
        => _db.ProviderSubscriptions
            .Where(x => x.Status == SubscriptionStatus.Active
                     && x.AutoRenew
                     && x.SubscriptionPeriodEnd >= fromUtc
                     && x.SubscriptionPeriodEnd <  toUtc)
            .ToListAsync(ct);

    public Task AddSubscriptionAsync(ProviderPlanSubscriptionEntity entity, CancellationToken ct)
        => _db.ProviderSubscriptions.AddAsync(entity, ct).AsTask();

    public void UpdateSubscription(ProviderPlanSubscriptionEntity entity)
        => _db.ProviderSubscriptions.Update(entity);

    public Task AddAsync(ProviderPlanEntity entity, CancellationToken ct)
        => _db.ProviderPlans.AddAsync(entity, ct).AsTask();

    public void Update(ProviderPlanEntity entity) => _db.ProviderPlans.Update(entity);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
