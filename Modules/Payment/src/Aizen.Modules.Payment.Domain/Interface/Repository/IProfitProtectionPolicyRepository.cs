using Aizen.Modules.Payment.Domain.Entities.ProfitProtection;

namespace Aizen.Modules.Payment.Domain.Interface.Repository;

public interface IProfitProtectionPolicyRepository
{
    /// <summary>
    /// Pure resolution (NO writes): the single Active policy for <paramref name="currency"/> at <paramref name="atUtc"/>.
    /// Throws ProfitProtectionPolicyConflict on overlap; returns null if none (caller → ConfigurationError).
    /// </summary>
    Task<ProfitProtectionPolicyEntity?> ResolveAsync(string currency, DateTime atUtc, CancellationToken ct = default);

    /// <summary>Create/Update single-active guard: first overlapping active policy of the same currency, or null.</summary>
    Task<ProfitProtectionPolicyEntity?> FindOverlappingActivePolicyAsync(
        ProfitProtectionPolicyEntity candidate, CancellationToken ct = default);

    Task<ProfitProtectionPolicyEntity?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<List<ProfitProtectionPolicyEntity>> GetAllAsync(CancellationToken ct = default);

    Task AddAsync(ProfitProtectionPolicyEntity entity, CancellationToken ct = default);
    void Update(ProfitProtectionPolicyEntity entity);
    Task SaveChangesAsync(CancellationToken ct = default);

    /// <summary>Generates the next policy code in "PPOL-YYYY-XXX" format.</summary>
    Task<string> GenerateCodeAsync(CancellationToken ct = default);
}

public interface IProfitProtectionEvaluationLogRepository
{
    /// <summary>Insert-only audit write.</summary>
    Task AddAsync(ProfitProtectionEvaluationLogEntity entity, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
