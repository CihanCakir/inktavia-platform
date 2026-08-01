using Aizen.Modules.Payment.Domain.Entities.CustomerBenefit;

namespace Aizen.Modules.Payment.Domain.Interface.Repository;

public interface ICustomerBenefitBudgetRepository
{
    /// <summary>Tracked load (for concurrency-guarded mutations).</summary>
    Task<CustomerBenefitBudgetEntity?> GetByIdAsync(long id, CancellationToken ct = default);

    Task<CustomerBenefitBudgetEntity?> GetActiveBySubscriptionAsync(
        long participantPlanSubscriptionId, DateTime atUtc, CancellationToken ct = default);

    Task<CustomerBenefitReservationEntity?> GetReservationByIdAsync(long reservationId, CancellationToken ct = default);

    /// <summary>Idempotency lookup: an existing reservation for the same budget + context ref.</summary>
    Task<CustomerBenefitReservationEntity?> GetReservationByContextRefAsync(
        long budgetId, string contextRef, CancellationToken ct = default);

    Task AddBudgetAsync(CustomerBenefitBudgetEntity budget, CancellationToken ct = default);
    Task AddReservationAsync(CustomerBenefitReservationEntity reservation, CancellationToken ct = default);

    /// <summary>Persists tracked changes; throws CustomerBenefitConcurrencyConflict on an optimistic-concurrency clash.</summary>
    Task SaveChangesConcurrencySafeAsync(CancellationToken ct = default);
}

public interface ICustomerBenefitBudgetPolicyRepository
{
    Task<CustomerBenefitBudgetPolicyEntity?> ResolveAsync(
        long customerPlanId, string currency, DateTime atUtc, CancellationToken ct = default);

    Task<CustomerBenefitBudgetPolicyEntity?> FindOverlappingActivePolicyAsync(
        CustomerBenefitBudgetPolicyEntity candidate, CancellationToken ct = default);

    Task<CustomerBenefitBudgetPolicyEntity?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<List<CustomerBenefitBudgetPolicyEntity>> GetAllAsync(CancellationToken ct = default);

    Task AddAsync(CustomerBenefitBudgetPolicyEntity entity, CancellationToken ct = default);
    void Update(CustomerBenefitBudgetPolicyEntity entity);
    Task SaveChangesAsync(CancellationToken ct = default);

    Task<bool> ExistsForPlanAsync(long customerPlanId, string currency, CancellationToken ct = default);

    Task<string> GenerateCodeAsync(CancellationToken ct = default);
}
