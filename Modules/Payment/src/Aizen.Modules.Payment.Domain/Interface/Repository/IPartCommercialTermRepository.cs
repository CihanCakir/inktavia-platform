using Aizen.Modules.Payment.Domain.Entities.PartCommercialTerm;

namespace Aizen.Modules.Payment.Domain.Interface.Repository;

/// <summary>BE-S5 — persistence for the versioned/scoped part commercial terms (Payment-internal; cost never leaves the module).</summary>
public interface IPartCommercialTermRepository
{
    /// <summary>The effective-date-filtered active terms for a currency at <paramref name="atUtc"/> (fed to the pure resolver).</summary>
    Task<List<PartCommercialTermEntity>> GetActiveAtAsync(string currencyCode, DateTime atUtc, CancellationToken ct = default);

    /// <summary>An existing active term that overlaps <paramref name="candidate"/> on the same scope + priority (create/update guard).</summary>
    Task<PartCommercialTermEntity?> FindOverlappingActiveTermAsync(PartCommercialTermEntity candidate, CancellationToken ct = default);

    /// <summary>Highest existing version for the exact scope key (any status) — the next append is this + 1.</summary>
    Task<int> GetMaxVersionForScopeAsync(PartCommercialTermEntity candidate, CancellationToken ct = default);

    Task<PartCommercialTermEntity?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<List<PartCommercialTermEntity>> GetAllAsync(CancellationToken ct = default);

    Task AddAsync(PartCommercialTermEntity entity, CancellationToken ct = default);
    void Update(PartCommercialTermEntity entity);
    Task SaveChangesAsync(CancellationToken ct = default);

    Task<string> GenerateCodeAsync(CancellationToken ct = default);
}
