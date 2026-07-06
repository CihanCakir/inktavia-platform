using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.CargoDry.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.CargoDry.Repository.Repositories;

public sealed class CargoDryProviderInventoryRepository : ICargoDryProviderInventoryRepository
{
    private readonly CargoDryDbContext _db;
    public CargoDryProviderInventoryRepository(CargoDryDbContext db) => _db = db;

    public Task<CargoDryProviderInventoryEntity?> GetByProviderProductBatchAsync(
        long              providerProfileId,
        string            productCode,
        string?           batchCode,
        CancellationToken ct)
        => _db.ProviderInventories.FirstOrDefaultAsync(x =>
            x.ProviderProfileId == providerProfileId
            && x.ProductCode    == productCode
            && x.BatchCode      == batchCode,
            ct);

    public async Task<IReadOnlyList<CargoDryProviderInventoryEntity>> GetByProviderAsync(
        long              providerProfileId,
        CancellationToken ct)
    {
        return await _db.ProviderInventories
            .AsNoTracking()
            .Where(x => x.ProviderProfileId == providerProfileId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(ct);
    }

    public async Task<(List<CargoDryProviderInventoryEntity> Items, int Total)> GetPagedAsync(
        long?                    providerProfileId,
        string?                  productCode,
        CargoDryCommercialModel? commercialModel,
        SalesChannel?            salesChannel,
        bool?                    hasAvailableStock,
        string?                  search,
        int                      skip,
        int                      take,
        CancellationToken        ct)
    {
        var query = _db.ProviderInventories.AsNoTracking().AsQueryable();

        if (providerProfileId.HasValue)
            query = query.Where(x => x.ProviderProfileId == providerProfileId.Value);

        if (!string.IsNullOrWhiteSpace(productCode))
            query = query.Where(x => x.ProductCode == productCode);

        if (commercialModel.HasValue)
            query = query.Where(x => x.CommercialModel == commercialModel.Value);

        if (salesChannel.HasValue)
            query = query.Where(x => x.SalesChannel == salesChannel.Value);

        if (hasAvailableStock.HasValue && hasAvailableStock.Value)
            query = query.Where(x =>
                (x.TotalAllocated + x.TotalAdjusted - x.TotalActivated - x.TotalRevoked - x.TotalReturned) > 0);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x =>
                x.ProductCode.Contains(search) ||
                (x.BatchCode != null && x.BatchCode.Contains(search)));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, total);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<long>> GetDistinctProviderProfileIdsAsync(
        CancellationToken ct)
    {
        var ids = await _db.ProviderInventories
            .Select(x => x.ProviderProfileId)
            .Distinct()
            .ToListAsync(ct);

        return ids;
    }

    public async Task AddAsync(CargoDryProviderInventoryEntity entity, CancellationToken ct)
    {
        await _db.ProviderInventories.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct)
        => _db.SaveChangesAsync(ct);
}
