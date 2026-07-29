using Aizen.Modules.Payment.Domain.Entities.CommissionBenefit;

namespace Aizen.Modules.Payment.Domain.Interface.Repository;

public interface IProviderCommissionBenefitRuleRepository
{
    /// <summary>Active + effective-at-<paramref name="atUtc"/> benefit rules for a currency (scope match is the resolver's job).</summary>
    Task<List<ProviderCommissionBenefitRuleEntity>> GetActiveCandidatesAsync(
        string currency, DateTime atUtc, CancellationToken ct = default);

    /// <summary>Create/Update conflict guard: first overlapping active rule of the same scope-key + priority, or null.</summary>
    Task<ProviderCommissionBenefitRuleEntity?> FindOverlappingActiveRuleAsync(
        ProviderCommissionBenefitRuleEntity candidate, CancellationToken ct = default);

    Task<ProviderCommissionBenefitRuleEntity?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<List<ProviderCommissionBenefitRuleEntity>> GetAllAsync(CancellationToken ct = default);

    Task AddAsync(ProviderCommissionBenefitRuleEntity entity, CancellationToken ct = default);
    void Update(ProviderCommissionBenefitRuleEntity entity);
    Task SaveChangesAsync(CancellationToken ct = default);

    Task<bool> ExistsByRuleCodeAsync(string ruleCode, CancellationToken ct = default);
    Task<string> GenerateRuleCodeAsync(CancellationToken ct = default);
}

public interface IProviderCommissionBenefitEntitlementRepository
{
    Task<ProviderCommissionBenefitEntitlementEntity?> GetByIdAsync(long id, CancellationToken ct = default);

    Task<ProviderCommissionBenefitEntitlementEntity?> GetActiveByProviderAndRuleAsync(
        long providerProfileId, long benefitRuleId, DateTime atUtc, CancellationToken ct = default);

    Task<ProviderCommissionBenefitUsageEntity?> GetUsageByIdAsync(long usageId, CancellationToken ct = default);
    Task<ProviderCommissionBenefitUsageEntity?> GetUsageByContextRefAsync(
        long entitlementId, string contextRef, CancellationToken ct = default);

    Task AddEntitlementAsync(ProviderCommissionBenefitEntitlementEntity entity, CancellationToken ct = default);
    Task AddUsageAsync(ProviderCommissionBenefitUsageEntity usage, CancellationToken ct = default);
    void UpdateEntitlement(ProviderCommissionBenefitEntitlementEntity entity);

    /// <summary>Persists tracked changes; maps an optimistic-concurrency clash → ProviderCommissionBenefitConcurrencyConflict.</summary>
    Task SaveChangesConcurrencySafeAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);

    Task<string> GenerateEntitlementCodeAsync(CancellationToken ct = default);
}
