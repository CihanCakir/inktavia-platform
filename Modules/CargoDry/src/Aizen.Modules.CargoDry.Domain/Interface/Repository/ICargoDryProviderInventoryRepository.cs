using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Entities;

namespace Aizen.Modules.CargoDry.Domain.Interface.Repository;

public interface ICargoDryProviderInventoryRepository
{
    /// <summary>
    /// Returns the inventory row for the given provider + product + batch combination.
    /// Used for idempotency check and update in AllocateBatchToProvider.
    /// </summary>
    Task<CargoDryProviderInventoryEntity?> GetByProviderProductBatchAsync(
        long              providerProfileId,
        string            productCode,
        string?           batchCode,
        CancellationToken ct = default);

    /// <summary>All inventory rows for a specific provider (all products/batches).</summary>
    Task<IReadOnlyList<CargoDryProviderInventoryEntity>> GetByProviderAsync(
        long              providerProfileId,
        CancellationToken ct = default);

    /// <summary>Paged list with optional filters.</summary>
    Task<(List<CargoDryProviderInventoryEntity> Items, int Total)> GetPagedAsync(
        long?                    providerProfileId,
        string?                  productCode,
        CargoDryCommercialModel? commercialModel,
        SalesChannel?            salesChannel,
        bool?                    hasAvailableStock,
        string?                  search,
        int                      skip,
        int                      take,
        CancellationToken        ct = default);

    Task AddAsync(CargoDryProviderInventoryEntity entity, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
