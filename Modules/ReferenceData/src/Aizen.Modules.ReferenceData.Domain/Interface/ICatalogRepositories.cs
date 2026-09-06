using Aizen.Modules.ReferenceData.Domain.Entities.Catalog;

namespace Aizen.Modules.ReferenceData.Domain.Interface;

public interface IVesselBrandRepository
{
    Task<VesselBrandEntity?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<VesselBrandEntity?> GetByCodeAsync(string code, CancellationToken ct = default);
    /// <summary>Case-insensitive dedupe lookup by normalized name key (<see cref="CatalogNameNormalizer.Key"/>).</summary>
    Task<VesselBrandEntity?> GetByNameKeyAsync(string nameKey, CancellationToken ct = default);
    Task<bool> ExistsByCodeAsync(string code, CancellationToken ct = default);
    Task<IReadOnlyList<VesselBrandEntity>> SearchAsync(string? search, bool onlyActive, int take, CancellationToken ct = default);
    Task<IReadOnlyList<VesselBrandEntity>> ListNeedsReviewAsync(int skip, int take, CancellationToken ct = default);
    Task AddAsync(VesselBrandEntity entity, CancellationToken ct = default);
    void Update(VesselBrandEntity entity);
}

public interface IVesselModelRepository
{
    Task<VesselModelEntity?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<VesselModelEntity?> GetByCodeAsync(long brandId, string code, CancellationToken ct = default);
    Task<VesselModelEntity?> GetByNameKeyAsync(long brandId, string nameKey, CancellationToken ct = default);
    Task<IReadOnlyList<VesselModelEntity>> SearchAsync(long brandId, string? search, string? typeCode, bool onlyActive, int take, CancellationToken ct = default);
    Task<IReadOnlyList<VesselModelEntity>> ListNeedsReviewAsync(int skip, int take, CancellationToken ct = default);
    Task AddAsync(VesselModelEntity entity, CancellationToken ct = default);
    void Update(VesselModelEntity entity);
}

public interface IEngineBrandRepository
{
    Task<EngineBrandEntity?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<EngineBrandEntity?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<EngineBrandEntity?> GetByNameKeyAsync(string nameKey, CancellationToken ct = default);
    Task<bool> ExistsByCodeAsync(string code, CancellationToken ct = default);
    Task<IReadOnlyList<EngineBrandEntity>> SearchAsync(string? search, bool onlyActive, int take, CancellationToken ct = default);
    Task<IReadOnlyList<EngineBrandEntity>> ListNeedsReviewAsync(int skip, int take, CancellationToken ct = default);
    Task AddAsync(EngineBrandEntity entity, CancellationToken ct = default);
    void Update(EngineBrandEntity entity);
}

public interface IEngineModelRepository
{
    Task<EngineModelEntity?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<EngineModelEntity?> GetByCodeAsync(long brandId, string code, CancellationToken ct = default);
    Task<EngineModelEntity?> GetByNameKeyAsync(long brandId, string nameKey, CancellationToken ct = default);
    Task<IReadOnlyList<EngineModelEntity>> SearchAsync(long brandId, string? search, string? typeCode, bool onlyActive, int take, CancellationToken ct = default);
    Task<IReadOnlyList<EngineModelEntity>> ListNeedsReviewAsync(int skip, int take, CancellationToken ct = default);
    Task AddAsync(EngineModelEntity entity, CancellationToken ct = default);
    void Update(EngineModelEntity entity);
}
