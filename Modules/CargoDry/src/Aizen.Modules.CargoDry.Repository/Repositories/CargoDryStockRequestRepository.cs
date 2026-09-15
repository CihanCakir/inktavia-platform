using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.CargoDry.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.CargoDry.Repository.Repositories;

public sealed class CargoDryStockRequestRepository : ICargoDryStockRequestRepository
{
    private readonly CargoDryDbContext _db;
    public CargoDryStockRequestRepository(CargoDryDbContext db) => _db = db;

    public Task<CargoDryStockRequestEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => _db.StockRequests.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<(List<CargoDryStockRequestEntity> Items, int Total)> GetByProviderAsync(
        long providerProfileId, CargoDryStockRequestStatus? status, int skip, int take, CancellationToken ct = default)
    {
        var q = _db.StockRequests.Where(x => x.ProviderProfileId == providerProfileId);
        if (status.HasValue) q = q.Where(x => x.Status == status.Value);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(x => x.CreateDate).Skip(skip).Take(take).ToListAsync(ct);
        return (items, total);
    }

    public async Task<(List<CargoDryStockRequestEntity> Items, int Total)> GetPagedAsync(
        CargoDryStockRequestStatus? status, long? providerProfileId, int skip, int take, CancellationToken ct = default)
    {
        var q = _db.StockRequests.AsQueryable();
        if (status.HasValue)            q = q.Where(x => x.Status == status.Value);
        if (providerProfileId is { } p) q = q.Where(x => x.ProviderProfileId == p);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(x => x.CreateDate).Skip(skip).Take(take).ToListAsync(ct);
        return (items, total);
    }

    public Task<List<CargoDryStockRequestEntity>> GetRecentByProviderAsync(long providerProfileId, int take, CancellationToken ct = default)
        => _db.StockRequests.AsNoTracking()
            .Where(x => x.ProviderProfileId == providerProfileId)
            .OrderByDescending(x => x.CreateDate).Take(take).ToListAsync(ct);

    public Task<int> CountByProviderAsync(long providerProfileId, CancellationToken ct = default)
        => _db.StockRequests.CountAsync(x => x.ProviderProfileId == providerProfileId, ct);

    public async Task<IReadOnlyList<long>> GetAutoReceiveDueIdsAsync(DateTimeOffset nowUtc, int maxBatch, CancellationToken ct = default)
        => await _db.StockRequests.AsNoTracking()
            .Where(x => x.Status == CargoDryStockRequestStatus.Shipped
                        && x.AutoReceiveDeadlineUtc != null
                        && x.AutoReceiveDeadlineUtc < nowUtc)
            .OrderBy(x => x.AutoReceiveDeadlineUtc)
            .Take(maxBatch)
            .Select(x => x.Id)
            .ToListAsync(ct);

    public Task<bool> HasPendingForProductAsync(long providerProfileId, string productCode, CancellationToken ct = default)
        => _db.StockRequests.AnyAsync(x =>
            x.ProviderProfileId == providerProfileId
            && x.ProductCode == productCode
            && x.Status == CargoDryStockRequestStatus.Pending, ct);

    public Task AddAsync(CargoDryStockRequestEntity entity, CancellationToken ct = default)
        => _db.StockRequests.AddAsync(entity, ct).AsTask();

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
