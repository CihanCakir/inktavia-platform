using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.CargoDry.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.CargoDry.Repository.Repositories;

public sealed class CargoDryDirectSaleRepository : ICargoDryDirectSaleRepository
{
    private readonly CargoDryDbContext _db;
    public CargoDryDirectSaleRepository(CargoDryDbContext db) => _db = db;

    public Task<CargoDryDirectSaleEntity?> GetBySourceServiceRequestIdAsync(long serviceRequestId, CancellationToken ct)
        => _db.DirectSales.FirstOrDefaultAsync(x => x.SourceServiceRequestId == serviceRequestId, ct);

    public async Task AddAsync(CargoDryDirectSaleEntity entity, CancellationToken ct)
        => await _db.DirectSales.AddAsync(entity, ct);

    public Task SaveChangesAsync(CancellationToken ct)
        => _db.SaveChangesAsync(ct);
}
