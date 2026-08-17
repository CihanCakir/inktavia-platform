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
