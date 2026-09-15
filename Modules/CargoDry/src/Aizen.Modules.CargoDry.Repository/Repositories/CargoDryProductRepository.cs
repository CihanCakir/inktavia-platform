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

    public async Task<CargoDryProductEntity?> GetByCodeWithImagesAsync(string productCode, CancellationToken ct)
    {
        var product = await _db.Products
            .Include(x => x.Images.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(x => x.ProductCode == productCode, ct);
        return product;
    }

    public Task<List<CargoDryProductEntity>> GetAllActiveWithImagesAsync(CancellationToken ct)
        => _db.Products
            .Where(x => x.IsActive)
            .Include(x => x.Images.OrderBy(i => i.SortOrder))
            .OrderBy(x => x.ProductCode)
            .ToListAsync(ct);

    public Task<List<CargoDryProductEntity>> GetAllWithImagesAsync(CancellationToken ct)
        => _db.Products
            .Include(x => x.Images.OrderBy(i => i.SortOrder))
            .OrderBy(x => x.ProductCode)
            .ToListAsync(ct);

    public Task<bool> ExistsByCodeAsync(string productCode, CancellationToken ct)
        => _db.Products.AnyAsync(x => x.ProductCode == productCode, ct);

    public async Task AddAsync(CargoDryProductEntity entity, CancellationToken ct)
    {
        await _db.Products.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
    }

    public void RemoveImage(CargoDryProductImageEntity image)
        => _db.ProductImages.Remove(image);

    public Task SaveChangesAsync(CancellationToken ct)
        => _db.SaveChangesAsync(ct);
}
