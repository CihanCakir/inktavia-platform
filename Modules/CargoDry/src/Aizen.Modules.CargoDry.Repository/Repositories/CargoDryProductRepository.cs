using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.CargoDry.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.CargoDry.Repository.Repositories;

public sealed class CargoDryProductRepository : ICargoDryProductRepository
{
    private readonly CargoDryDbContext _db;
    public CargoDryProductRepository(CargoDryDbContext db) => _db = db;

    public Task<CargoDryProductEntity?> GetByCodeAsync(string productCode, CancellationToken ct)
        => _db.Products.FirstOrDefaultAsync(x => x.ProductCode == productCode, ct);

    public Task<List<CargoDryProductEntity>> GetAllActiveAsync(CancellationToken ct)
        => _db.Products.Where(x => x.IsActive).ToListAsync(ct);

    public Task<List<CargoDryProductEntity>> GetAllAsync(CancellationToken ct)
        => _db.Products.OrderBy(x => x.ProductCode).ToListAsync(ct);

    public async Task AddAsync(CargoDryProductEntity entity, CancellationToken ct)
    {
        await _db.Products.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
    }
}
