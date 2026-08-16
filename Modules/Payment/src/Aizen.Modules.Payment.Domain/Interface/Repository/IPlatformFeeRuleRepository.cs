using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.PlatformFee;

namespace Aizen.Modules.Payment.Domain.Interface.Repository;

public interface IPlatformFeeRuleRepository
{
    // ── Resolution (pure, BE-P3) ───────────────────────────────────────────────

    /// <summary>
    /// Pure resolution (NO writes): resolves the most-specific active rule for <paramref name="ctx"/> via the
    /// specificity matrix + Priority tie-break. Throws <c>PlatformFeeRuleConflict</c> on a fail-loud tie.
    /// Returns null when nothing matches.
    /// </summary>
    Task<PlatformFeeResolution?> ResolveAsync(
        PlatformFeeResolveContext ctx, DateTime atUtc, CancellationToken ct = default);

    /// <summary>
    /// Create/Update conflict guard (§7): returns the first existing active rule that overlaps
    /// <paramref name="candidate"/> (same scope-key + Priority + overlapping window), or null.
    /// </summary>
    Task<PlatformFeeRuleEntity?> FindOverlappingActiveRuleAsync(
        PlatformFeeRuleEntity candidate, CancellationToken ct = default);

    // ── Read ────────────────────────────────────────────────────────────────────

    Task<List<PlatformFeeRuleEntity>> GetAllAsync(CancellationToken ct = default);

    Task<PlatformFeeRuleEntity?> GetByIdAsync(long id, CancellationToken ct = default);

    /// <summary>
    /// Returns the Global platform-fee rule for the public pricing surface: no <c>CategoryCode</c>, no
    /// <c>CustomerType</c>, matching <paramref name="currencyCode"/>, active, and effective at
    /// <paramref name="atUtc"/> (<c>EffectiveFrom &lt;= atUtc AND (EffectiveTo == null OR atUtc &lt; EffectiveTo)</c>) —
    /// the latest <c>EffectiveFrom</c> among those. This is the customer-quoted headline fee; category/customer-type
    /// overrides are internal and NOT returned. Null when none resolves (do not fabricate).
    /// </summary>
    Task<PlatformFeeRuleEntity?> GetGlobalRuleAsync(string currencyCode, DateTime atUtc, CancellationToken ct = default);

    Task<(List<PlatformFeeRuleEntity> Items, int Total)> GetPagedAsync(
        PlatformFeeModel?     model,
        CommissionRuleStatus? status,
        string?               currencyCode,
        string?               categoryCode,
        string?               customerType,
        int                   skip,
        int                   take,
        CancellationToken     ct = default);

    // ── Write ─────────────────────────────────────────────────────────────────

    Task AddAsync(PlatformFeeRuleEntity entity, CancellationToken ct = default);
    void Update(PlatformFeeRuleEntity entity);
    void Remove(PlatformFeeRuleEntity entity);
    Task SaveChangesAsync(CancellationToken ct = default);

    // ── Code generation ───────────────────────────────────────────────────────

    /// <summary>Generates the next rule code in "PFR-YYYY-XXX" format.</summary>
    Task<string> GenerateRuleCodeAsync(CancellationToken ct = default);
}
