using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.CargoDry.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.CargoDry.Repository.Repositories;

public sealed class CargoDrySalesAttributionRepository : ICargoDrySalesAttributionRepository
{
    private readonly CargoDryDbContext _db;
    public CargoDrySalesAttributionRepository(CargoDryDbContext db) => _db = db;

    public Task<CargoDrySalesAttributionEntity?> GetByIdAsync(long id, CancellationToken ct)
        => _db.SalesAttributions.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<CargoDrySalesAttributionEntity?> GetByKitIdAsync(long kitId, CancellationToken ct)
        => _db.SalesAttributions.FirstOrDefaultAsync(x => x.KitId == kitId, ct);

    public async Task<(List<CargoDrySalesAttributionEntity> Items, int Total)> GetPagedAsync(
        long?                           providerProfileId,
        string?                         productCode,
        string?                         batchCode,
        SalesChannel?                   salesChannel,
        CargoDryCommercialModel?        commercialModel,
        CargoDrySalesAttributionStatus? status,
        long?                           settlementId,
        DateTime?                       dateFrom,
        DateTime?                       dateTo,
        string?                         search,
        int                             skip,
        int                             take,
        CancellationToken               ct)
    {
        var query = _db.SalesAttributions.AsNoTracking().AsQueryable();

        if (providerProfileId.HasValue)
            query = query.Where(x => x.ProviderProfileId == providerProfileId.Value);

        if (!string.IsNullOrWhiteSpace(productCode))
            query = query.Where(x => x.ProductCode == productCode);

        if (!string.IsNullOrWhiteSpace(batchCode))
            query = query.Where(x => x.BatchCode == batchCode);

        if (salesChannel.HasValue)
            query = query.Where(x => x.SalesChannel == salesChannel.Value);

        if (commercialModel.HasValue)
            query = query.Where(x => x.CommercialModel == commercialModel.Value);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (settlementId.HasValue)
            query = query.Where(x => x.SellThroughSettlementId == settlementId.Value);

        if (dateFrom.HasValue)
            query = query.Where(x => x.CreatedAtUtc >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(x => x.CreatedAtUtc <= dateTo.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x =>
                x.SerialNumber.Contains(search) ||
                x.KitCode.Contains(search)      ||
                x.ProductCode.Contains(search));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, total);
    }

    /// <inheritdoc cref="ICargoDrySalesAttributionRepository.GetBySettlementIdAsync"/>
    public async Task<IReadOnlyList<CargoDrySalesAttributionEntity>> GetBySettlementIdAsync(
        long settlementId, CancellationToken ct)
        => await _db.SalesAttributions
            .Where(x => x.SellThroughSettlementId == settlementId)
            .ToListAsync(ct);

    /// <inheritdoc cref="ICargoDrySalesAttributionRepository.GetUnresolvedBySettlementIdAsync"/>
    public async Task<IReadOnlyList<CargoDrySalesAttributionEntity>> GetUnresolvedBySettlementIdAsync(
        long settlementId, CancellationToken ct)
        => await _db.SalesAttributions
            .Where(x => x.SellThroughSettlementId == settlementId &&
                        x.FinancialResolvedAtUtc == null)
            .ToListAsync(ct);

    /// <inheritdoc cref="ICargoDrySalesAttributionRepository.GetCommissionRuleUsageGroupedAsync"/>
    public async Task<(List<CargoDryCommissionRuleUsageRowDto> Items, int Total)> GetCommissionRuleUsageGroupedAsync(
        DateTime?         dateFrom,
        DateTime?         dateTo,
        long?             ruleId,
        string?           productCode,
        string?           salesChannel,
        long?             providerProfileId,
        int               skip,
        int               take,
        CancellationToken ct)
    {
        var query = _db.SalesAttributions.AsNoTracking()
            .Where(x => x.ResolvedRuleId != null || x.ResolvedRuleSource != null);

        if (dateFrom.HasValue)
            query = query.Where(x => x.CreatedAtUtc >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(x => x.CreatedAtUtc <= dateTo.Value);

        if (ruleId.HasValue)
            query = query.Where(x => x.ResolvedRuleId == ruleId.Value);

        if (!string.IsNullOrWhiteSpace(productCode))
            query = query.Where(x => x.ProductCode == productCode);

        if (!string.IsNullOrWhiteSpace(salesChannel) &&
            System.Enum.TryParse<SalesChannel>(salesChannel, ignoreCase: true, out var channelEnum))
            query = query.Where(x => x.SalesChannel == channelEnum);

        if (providerProfileId.HasValue)
            query = query.Where(x => x.ProviderProfileId == providerProfileId.Value);

        var grouped = query.GroupBy(x => new
        {
            x.ResolvedRuleId,
            x.ResolvedRuleName,
            x.ResolvedRuleSource,
            x.ProductCode,
            x.SalesChannel,
            x.ProviderProfileId,
        })
        .Select(g => new CargoDryCommissionRuleUsageRowDto
        {
            RuleId                   = g.Key.ResolvedRuleId,
            RuleName                 = g.Key.ResolvedRuleName,
            ResolvedRuleSource       = g.Key.ResolvedRuleSource,
            ProductCode              = g.Key.ProductCode,
            SalesChannel             = g.Key.SalesChannel.ToString(),
            ProviderProfileId        = g.Key.ProviderProfileId,
            UsageCount               = g.Count(),
            TotalSaleAmount          = g.Sum(x => x.SalePrice          ?? 0m),
            TotalProviderShareAmount = g.Sum(x => x.ProviderShareAmount ?? 0m),
            TotalPlatformShareAmount = g.Sum(x => x.PlatformShareAmount ?? 0m),
            FirstUsedAtUtc           = g.Min(x => (DateTime?)x.CreatedAtUtc),
            LastUsedAtUtc            = g.Max(x => (DateTime?)x.CreatedAtUtc),
        });

        var total = await grouped.CountAsync(ct);
        var items = await grouped
            .OrderByDescending(g => g.UsageCount)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task AddAsync(CargoDrySalesAttributionEntity entity, CancellationToken ct)
    {
        await _db.SalesAttributions.AddAsync(entity, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct)
        => _db.SaveChangesAsync(ct);
}
