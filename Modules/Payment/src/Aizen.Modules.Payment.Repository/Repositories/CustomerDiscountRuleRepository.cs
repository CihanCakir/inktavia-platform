using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.CustomerDiscount;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Payment.Repository.Repositories;

/// <summary>Persistence + pure resolution for <see cref="CustomerDiscountRuleEntity"/> (BE-P6).</summary>
public sealed class CustomerDiscountRuleRepository : ICustomerDiscountRuleRepository
{
    private readonly PaymentDbContext _db;
    public CustomerDiscountRuleRepository(PaymentDbContext db) => _db = db;

    public async Task<CustomerDiscountRuleEntity?> ResolveAsync(
        CustomerDiscountResolveContext ctx, DateTime atUtc, CancellationToken ct = default)
    {
        var cur = ctx.CurrencyCode.ToUpperInvariant();
        var active = await _db.CustomerDiscountRules
            .AsNoTracking()
            .Where(x => x.CurrencyCode == cur && x.IsActive
                     && x.EffectiveFrom <= atUtc && (x.EffectiveTo == null || x.EffectiveTo > atUtc))
            .ToListAsync(ct);

        return CustomerDiscountRuleResolver.Resolve(active, ctx);
    }

    public async Task<CustomerDiscountRuleEntity?> FindOverlappingActiveRuleAsync(
        CustomerDiscountRuleEntity candidate, CancellationToken ct = default)
    {
        var active = await _db.CustomerDiscountRules.AsNoTracking().Where(x => x.IsActive).ToListAsync(ct);
        return CustomerDiscountRuleResolver.FindOverlappingConflict(candidate, active);
    }

    public Task<CustomerDiscountRuleEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => _db.CustomerDiscountRules.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<List<CustomerDiscountRuleEntity>> GetAllAsync(CancellationToken ct = default)
        => _db.CustomerDiscountRules.OrderByDescending(x => x.EffectiveFrom).ToListAsync(ct);

    public Task AddAsync(CustomerDiscountRuleEntity entity, CancellationToken ct = default)
        => _db.CustomerDiscountRules.AddAsync(entity, ct).AsTask();

    public void Update(CustomerDiscountRuleEntity entity) => _db.CustomerDiscountRules.Update(entity);
    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);

    public Task<bool> ExistsPlatformFundedPlanRuleAsync(long customerPlanId, string currency, CancellationToken ct = default)
    {
        var cur = currency.ToUpperInvariant();
        return _db.CustomerDiscountRules.AnyAsync(x =>
            x.CustomerPlanId == customerPlanId && x.CurrencyCode == cur && x.CategoryCode == null
            && x.FundingMode == CustomerDiscountFundingMode.PlatformFunded, ct);
    }

    public async Task<string> GenerateCodeAsync(CancellationToken ct = default)
    {
        var year  = DateTime.UtcNow.Year;
        var count = await _db.CustomerDiscountRules.CountAsync(ct);
        var suffix = ((char)('A' + (count % 26))).ToString();
        return $"CDR-{year}-{suffix}{(count + 1):D3}";
    }
}
