using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Commission;

namespace Aizen.Modules.Payment.Domain.Interface.Repository;

public interface ICommissionRuleRepository
{
    /// <summary>
    /// Resolves the effective commission rate using precedence:
    /// ProviderOverride > Plan > Category > Global.
    /// Returns the rate (e.g. 0.12 for 12%) or null if no rule found.
    /// </summary>
    Task<decimal?> ResolveRateAsync(
        long?   providerProfileId,
        long?   providerPlanId,
        string? categoryCode,
        DateTime atUtc,
        CancellationToken ct = default);

    Task<List<CommissionRuleEntity>> GetAllAsync(CancellationToken ct = default);
    Task<CommissionRuleEntity?> GetByIdAsync(long id, CancellationToken ct = default);

    Task AddAsync(CommissionRuleEntity entity, CancellationToken ct = default);
    void Update(CommissionRuleEntity entity);
    void Remove(CommissionRuleEntity entity);
    Task SaveChangesAsync(CancellationToken ct = default);
}
