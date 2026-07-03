using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Plan;
using Aizen.Modules.Payment.Domain.Entities.Subscription;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Payment.Repository.Repositories;

public sealed class ParticipantPlanRepository : IParticipantPlanRepository
{
    private readonly PaymentDbContext _db;
    public ParticipantPlanRepository(PaymentDbContext db) => _db = db;

    public Task<List<ParticipantPlanEntity>> GetAllActiveAsync(CancellationToken ct)
        => _db.ParticipantPlans.Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToListAsync(ct);

    public Task<ParticipantPlanEntity?> GetByIdAsync(long id, CancellationToken ct)
        => _db.ParticipantPlans.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<ParticipantPlanEntity?> GetByCodeAsync(string planCode, CancellationToken ct)
        => _db.ParticipantPlans.FirstOrDefaultAsync(x => x.PlanCode == planCode, ct);

    public Task<bool> ExistsByCodeAsync(string planCode, CancellationToken ct)
        => _db.ParticipantPlans.AnyAsync(x => x.PlanCode == planCode, ct);

    public Task<ParticipantPlanSubscriptionEntity?> GetActiveSubscriptionAsync(long participantProfileId, DateTime atUtc, CancellationToken ct)
        => _db.ParticipantSubscriptions.FirstOrDefaultAsync(x =>
            x.ParticipantProfileId == participantProfileId &&
            x.Status == SubscriptionStatus.Active &&
            x.SubscriptionPeriodStart <= atUtc &&
            x.SubscriptionPeriodEnd >= atUtc, ct);

    public Task<ParticipantPlanSubscriptionEntity?> GetSubscriptionByTransactionIdAsync(long transactionId, CancellationToken ct)
        => _db.ParticipantSubscriptions.FirstOrDefaultAsync(
            x => x.PaymentTransactionId == transactionId, ct);

    public Task<List<ParticipantPlanSubscriptionEntity>> GetExpiredActiveSubscriptionsAsync(
        int batchSize, CancellationToken ct)
        => _db.ParticipantSubscriptions
            .Where(x => x.Status == SubscriptionStatus.Active
                     && x.SubscriptionPeriodEnd < DateTime.UtcNow)
            .OrderBy(x => x.SubscriptionPeriodEnd)
            .Take(batchSize)
            .ToListAsync(ct);

    public Task<List<ParticipantPlanSubscriptionEntity>> GetPastDueSubscriptionsAsync(
        int batchSize, CancellationToken ct)
        => _db.ParticipantSubscriptions
            .Where(x => x.Status == SubscriptionStatus.PastDue)
            .OrderBy(x => x.CreateDate)
            .Take(batchSize)
            .ToListAsync(ct);

    public Task<int> CountActiveSubscriptionsAsync(DateTime utcNow, CancellationToken ct)
        => _db.ParticipantSubscriptions.CountAsync(x =>
            x.Status == SubscriptionStatus.Active &&
            x.SubscriptionPeriodStart <= utcNow &&
            x.SubscriptionPeriodEnd   >= utcNow, ct);

    public Task<int> CountExpiringSoonAsync(DateTime utcNow, int withinDays, CancellationToken ct)
    {
        var threshold = utcNow.AddDays(withinDays);
        return _db.ParticipantSubscriptions.CountAsync(x =>
            x.Status == SubscriptionStatus.Active &&
            x.SubscriptionPeriodEnd >= utcNow &&
            x.SubscriptionPeriodEnd <= threshold, ct);
    }

    public Task<int> CountPastDueThisMonthAsync(DateTime monthStart, DateTime monthEnd, CancellationToken ct)
        => _db.ParticipantSubscriptions.CountAsync(x =>
            x.Status == SubscriptionStatus.PastDue &&
            x.CreateDate >= monthStart &&
            x.CreateDate <  monthEnd, ct);

    public async Task<decimal> SumActiveMrrAsync(DateTime utcNow, CancellationToken ct)
    {
        var amounts = await _db.ParticipantSubscriptions
            .Where(x => x.Status == SubscriptionStatus.Active &&
                        x.SubscriptionPeriodStart <= utcNow &&
                        x.SubscriptionPeriodEnd   >= utcNow)
            .Select(x => x.PaidAmount)
            .ToListAsync(ct);
        return amounts.Sum();
    }

    public Task<int> CountTotalPastDueAsync(CancellationToken ct)
        => _db.ParticipantSubscriptions.CountAsync(x => x.Status == SubscriptionStatus.PastDue, ct);

    public async Task<List<(int Year, int Month, decimal Total)>> GetMonthlyPaidAmountAsync(
        DateTime fromUtc, CancellationToken ct)
    {
        var raw = await _db.ParticipantSubscriptions
            .Where(x => x.CreateDate >= fromUtc)
            .Select(x => new { x.CreateDate, x.PaidAmount })
            .ToListAsync(ct);

        return raw
            .Where(x => x.CreateDate.HasValue)
            .GroupBy(x => new { x.CreateDate!.Value.Year, x.CreateDate!.Value.Month })
            .Select(g => (g.Key.Year, g.Key.Month, g.Sum(x => x.PaidAmount)))
            .ToList();
    }

    public async Task<List<ParticipantPlanSubscriptionEntity>> GetAllSubscriptionsForAdminAsync(
        string? status, CancellationToken ct)
    {
        var query = _db.ParticipantSubscriptions.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<SubscriptionStatus>(status, ignoreCase: true, out var parsedStatus))
        {
            query = query.Where(x => x.Status == parsedStatus);
        }
        return await query.OrderByDescending(x => x.CreateDate).ToListAsync(ct);
    }

    public Task AddSubscriptionAsync(ParticipantPlanSubscriptionEntity entity, CancellationToken ct)
        => _db.ParticipantSubscriptions.AddAsync(entity, ct).AsTask();

    public void UpdateSubscription(ParticipantPlanSubscriptionEntity entity)
        => _db.ParticipantSubscriptions.Update(entity);

    public Task AddAsync(ParticipantPlanEntity entity, CancellationToken ct)
        => _db.ParticipantPlans.AddAsync(entity, ct).AsTask();

    public void Update(ParticipantPlanEntity entity) => _db.ParticipantPlans.Update(entity);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
