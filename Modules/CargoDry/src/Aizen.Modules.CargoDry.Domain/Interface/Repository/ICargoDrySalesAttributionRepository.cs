using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Entities;

namespace Aizen.Modules.CargoDry.Domain.Interface.Repository;

public interface ICargoDrySalesAttributionRepository
{
    Task<CargoDrySalesAttributionEntity?> GetByIdAsync(long id, CancellationToken ct);

    Task<CargoDrySalesAttributionEntity?> GetByKitIdAsync(long kitId, CancellationToken ct);

    Task<(List<CargoDrySalesAttributionEntity> Items, int Total)> GetPagedAsync(
        long?                          providerProfileId,
        string?                        productCode,
        string?                        batchCode,
        SalesChannel?                  salesChannel,
        CargoDryCommercialModel?       commercialModel,
        CargoDrySalesAttributionStatus? status,
        long?                          settlementId,
        DateTime?                      dateFrom,
        DateTime?                      dateTo,
        string?                        search,
        int                            skip,
        int                            take,
        CancellationToken              ct);

    Task AddAsync(CargoDrySalesAttributionEntity entity, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
