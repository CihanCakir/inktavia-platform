using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.CargoDry.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.CargoDry.Repository.Repositories;

public sealed class CargoDryBatchRepository : ICargoDryBatchRepository
{
    private readonly CargoDryDbContext _db;
    public CargoDryBatchRepository(CargoDryDbContext db) => _db = db;

    public Task<CargoDryBatchEntity?> GetByCodeAsync(string batchCode, CancellationToken ct)
        => _db.Batches.FirstOrDefaultAsync(x => x.BatchCode == batchCode, ct);

    public Task<List<CargoDryBatchEntity>> GetAllAsync(CancellationToken ct)
        => _db.Batches.OrderByDescending(x => x.CreatedAt).ToListAsync(ct);

    public Task<List<CargoDryBatchEntity>> GetAllForReportAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
        => _db.Batches
            .AsNoTracking()
            .Where(b => b.CreatedAt >= from && b.CreatedAt <= to)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(CargoDryBatchEntity entity, CancellationToken ct)
    {
        await _db.Batches.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct)
        => _db.SaveChangesAsync(ct);
}
