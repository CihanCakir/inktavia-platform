using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.CustomerBenefit;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Payment.Repository.Repositories;

/// <summary>
/// Budget + reservation persistence (BE-P6). <see cref="SaveChangesConcurrencySafeAsync"/> translates an EF
/// optimistic-concurrency clash (someone else mutated the budget's Version first) into a business error, so two
/// concurrent checkouts cannot double-spend the same budget (§8/§19.15).
/// </summary>
public sealed class CustomerBenefitBudgetRepository : ICustomerBenefitBudgetRepository
{
    private readonly PaymentDbContext _db;
    public CustomerBenefitBudgetRepository(PaymentDbContext db) => _db = db;

    public Task<CustomerBenefitBudgetEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => _db.CustomerBenefitBudgets.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<CustomerBenefitBudgetEntity?> GetActiveBySubscriptionAsync(
        long participantPlanSubscriptionId, DateTime atUtc, CancellationToken ct = default)
        => _db.CustomerBenefitBudgets.FirstOrDefaultAsync(x =>
               x.ParticipantPlanSubscriptionId == participantPlanSubscriptionId
               && x.Status == CustomerBenefitBudgetStatus.Active
               && x.PeriodStart <= atUtc && x.PeriodEnd > atUtc, ct);

    public Task<CustomerBenefitReservationEntity?> GetReservationByIdAsync(long reservationId, CancellationToken ct = default)
        => _db.CustomerBenefitReservations.FirstOrDefaultAsync(x => x.Id == reservationId, ct);

    public Task<CustomerBenefitReservationEntity?> GetReservationByContextRefAsync(
        long budgetId, string contextRef, CancellationToken ct = default)
        => _db.CustomerBenefitReservations.FirstOrDefaultAsync(x => x.BudgetId == budgetId && x.ContextRef == contextRef, ct);

    public Task AddBudgetAsync(CustomerBenefitBudgetEntity budget, CancellationToken ct = default)
        => _db.CustomerBenefitBudgets.AddAsync(budget, ct).AsTask();

    public Task AddReservationAsync(CustomerBenefitReservationEntity reservation, CancellationToken ct = default)
        => _db.CustomerBenefitReservations.AddAsync(reservation, ct).AsTask();

    public async Task SaveChangesConcurrencySafeAsync(CancellationToken ct = default)
    {
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AizenBusinessException(
                (int)PaymentErrorCode.CustomerBenefitConcurrencyConflict,
                "The benefit budget was modified by another checkout — retry after reloading (optimistic concurrency).");
        }
    }
}

/// <summary>Persistence + pure resolution for <see cref="CustomerBenefitBudgetPolicyEntity"/> (BE-P6).</summary>
public sealed class CustomerBenefitBudgetPolicyRepository : ICustomerBenefitBudgetPolicyRepository
{
    private readonly PaymentDbContext _db;
    public CustomerBenefitBudgetPolicyRepository(PaymentDbContext db) => _db = db;

    public async Task<CustomerBenefitBudgetPolicyEntity?> ResolveAsync(
        long customerPlanId, string currency, DateTime atUtc, CancellationToken ct = default)
    {
        var cur = currency.ToUpperInvariant();
        var active = await _db.CustomerBenefitBudgetPolicies
            .AsNoTracking()
            .Where(x => x.CustomerPlanId == customerPlanId && x.CurrencyCode == cur && x.IsActive)
            .ToListAsync(ct);

        return CustomerBenefitBudgetPolicyResolver.Resolve(active, customerPlanId, cur, atUtc);
    }

    public async Task<CustomerBenefitBudgetPolicyEntity?> FindOverlappingActivePolicyAsync(
        CustomerBenefitBudgetPolicyEntity candidate, CancellationToken ct = default)
    {
        var active = await _db.CustomerBenefitBudgetPolicies
            .AsNoTracking()
            .Where(x => x.CustomerPlanId == candidate.CustomerPlanId && x.CurrencyCode == candidate.CurrencyCode && x.IsActive)
            .ToListAsync(ct);
        return CustomerBenefitBudgetPolicyResolver.FindOverlappingConflict(candidate, active);
    }

    public Task<CustomerBenefitBudgetPolicyEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => _db.CustomerBenefitBudgetPolicies.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<List<CustomerBenefitBudgetPolicyEntity>> GetAllAsync(CancellationToken ct = default)
        => _db.CustomerBenefitBudgetPolicies.OrderByDescending(x => x.EffectiveFrom).ToListAsync(ct);

    public Task AddAsync(CustomerBenefitBudgetPolicyEntity entity, CancellationToken ct = default)
        => _db.CustomerBenefitBudgetPolicies.AddAsync(entity, ct).AsTask();

    public void Update(CustomerBenefitBudgetPolicyEntity entity) => _db.CustomerBenefitBudgetPolicies.Update(entity);
    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);

    public Task<bool> ExistsForPlanAsync(long customerPlanId, string currency, CancellationToken ct = default)
    {
        var cur = currency.ToUpperInvariant();
        return _db.CustomerBenefitBudgetPolicies.AnyAsync(x => x.CustomerPlanId == customerPlanId && x.CurrencyCode == cur, ct);
    }

    public async Task<string> GenerateCodeAsync(CancellationToken ct = default)
    {
        var year  = DateTime.UtcNow.Year;
        var count = await _db.CustomerBenefitBudgetPolicies.CountAsync(ct);
        var suffix = ((char)('A' + (count % 26))).ToString();
        return $"CBP-{year}-{suffix}{(count + 1):D3}";
    }
}
