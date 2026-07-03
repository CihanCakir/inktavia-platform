using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.CargoDry.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.CargoDry.Repository.Repositories;

public sealed class CargoDryInventoryMovementRepository : ICargoDryInventoryMovementRepository
{
    private readonly CargoDryDbContext _db;
    public CargoDryInventoryMovementRepository(CargoDryDbContext db) => _db = db;

    public async Task<(List<CargoDryInventoryMovementEntity> Items, int Total)> GetPagedAsync(
        long?                  providerProfileId,
        string?                productCode,
        string?                batchCode,
        InventoryMovementType? movementType,
        DateTime?              dateFrom,
        DateTime?              dateTo,
        int                    skip,
        int                    take,
        CancellationToken      ct)
    {
        var query = _db.InventoryMovements.AsNoTracking().AsQueryable();

        if (providerProfileId.HasValue)
            query = query.Where(x => x.ProviderProfileId == providerProfileId.Value);

        if (!string.IsNullOrWhiteSpace(productCode))
            query = query.Where(x => x.ProductCode == productCode);

        if (!string.IsNullOrWhiteSpace(batchCode))
            query = query.Where(x => x.BatchCode == batchCode);

        if (movementType.HasValue)
            query = query.Where(x => x.MovementType == movementType.Value);

        if (dateFrom.HasValue)
            query = query.Where(x => x.CreatedAtUtc >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(x => x.CreatedAtUtc <= dateTo.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task AddAsync(CargoDryInventoryMovementEntity entity, CancellationToken ct)
    {
        await _db.InventoryMovements.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct)
        => _db.SaveChangesAsync(ct);
}
