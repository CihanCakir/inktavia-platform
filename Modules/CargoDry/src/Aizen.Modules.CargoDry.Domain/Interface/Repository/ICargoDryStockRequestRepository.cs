using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Entities;

namespace Aizen.Modules.CargoDry.Domain.Interface.Repository;

public interface ICargoDryStockRequestRepository
{
    Task<CargoDryStockRequestEntity?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<(List<CargoDryStockRequestEntity> Items, int Total)> GetByProviderAsync(
        long providerProfileId, CargoDryStockRequestStatus? status, int skip, int take, CancellationToken ct = default);
    Task<bool> HasPendingForProductAsync(long providerProfileId, string productCode, CancellationToken ct = default);
    Task AddAsync(CargoDryStockRequestEntity entity, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
