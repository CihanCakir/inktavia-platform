using Aizen.Modules.Payment.Domain.Entities.CustomerDiscount;

namespace Aizen.Modules.Payment.Domain.Interface.Repository;

public interface ICustomerDiscountRuleRepository
{
    /// <summary>Pure resolution (NO writes): the most-specific active rule for the context; tie → CustomerDiscountRuleConflict.</summary>
    Task<CustomerDiscountRuleEntity?> ResolveAsync(
        CustomerDiscountResolveContext ctx, DateTime atUtc, CancellationToken ct = default);

    /// <summary>Create/Update conflict guard: first overlapping active rule of the same scope-key + priority, or null.</summary>
    Task<CustomerDiscountRuleEntity?> FindOverlappingActiveRuleAsync(
        CustomerDiscountRuleEntity candidate, CancellationToken ct = default);

    Task<CustomerDiscountRuleEntity?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<List<CustomerDiscountRuleEntity>> GetAllAsync(CancellationToken ct = default);

    Task AddAsync(CustomerDiscountRuleEntity entity, CancellationToken ct = default);
    void Update(CustomerDiscountRuleEntity entity);
    Task SaveChangesAsync(CancellationToken ct = default);

    Task<bool> ExistsPlatformFundedPlanRuleAsync(long customerPlanId, string currency, CancellationToken ct = default);

    /// <summary>Generates the next rule code in "CDR-YYYY-XXX" format.</summary>
    Task<string> GenerateCodeAsync(CancellationToken ct = default);
}
