using Aizen.Modules.CargoDry.Domain.Entities;

namespace Aizen.Modules.CargoDry.Domain.Interface.Repository;

public interface ICargoDryProductRepository
{
    Task<CargoDryProductEntity?> GetByCodeAsync(string productCode, CancellationToken ct = default);
    Task<List<CargoDryProductEntity>> GetAllActiveAsync(CancellationToken ct = default);
    /// <summary>Returns all products including inactive — used by admin product catalog.</summary>
    Task<List<CargoDryProductEntity>> GetAllAsync(CancellationToken ct = default);

    // ── CargoDry supply flow (additive) — media-aware reads (Include ordered Images) ──
    /// <summary>Single product with its ordered gallery images loaded. Used by admin media management + product detail.</summary>
    Task<CargoDryProductEntity?> GetByCodeWithImagesAsync(string productCode, CancellationToken ct = default);
    /// <summary>Active products with their ordered gallery images loaded. Used by the owner-safe mobile catalog.</summary>
    Task<List<CargoDryProductEntity>> GetAllActiveWithImagesAsync(CancellationToken ct = default);
    /// <summary>All products (incl. inactive) with their ordered gallery images. Backs the media-aware product list.</summary>
    Task<List<CargoDryProductEntity>> GetAllWithImagesAsync(CancellationToken ct = default);

    /// <summary>Deletes a single product image row (after it has been removed from the product aggregate).</summary>
    void RemoveImage(CargoDryProductImageEntity image);
    Task AddAsync(CargoDryProductEntity entity, CancellationToken ct = default);
    Task<bool> ExistsByCodeAsync(string productCode, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
