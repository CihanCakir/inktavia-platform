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

    public async Task AddAsync(CargoDrySalesAttributionEntity entity, CancellationToken ct)
    {
        await _db.SalesAttributions.AddAsync(entity, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct)
        => _db.SaveChangesAsync(ct);
}
