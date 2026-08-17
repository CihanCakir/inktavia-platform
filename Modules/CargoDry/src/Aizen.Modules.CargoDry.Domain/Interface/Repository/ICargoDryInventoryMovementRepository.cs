using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Entities;

namespace Aizen.Modules.CargoDry.Domain.Interface.Repository;

public interface ICargoDryInventoryMovementRepository
{
    Task<(List<CargoDryInventoryMovementEntity> Items, int Total)> GetPagedAsync(
        long?                  providerProfileId,
        string?                productCode,
        string?                batchCode,
        InventoryMovementType? movementType,
        DateTime?              dateFrom,
        DateTime?              dateTo,
        int                    skip,
        int                    take,
        CancellationToken      ct = default);

    Task AddAsync(CargoDryInventoryMovementEntity entity, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
