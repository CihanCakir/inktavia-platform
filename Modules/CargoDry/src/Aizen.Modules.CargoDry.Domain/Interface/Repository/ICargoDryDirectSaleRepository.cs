using Aizen.Modules.CargoDry.Domain.Entities;

namespace Aizen.Modules.CargoDry.Domain.Interface.Repository;

/// <summary>CargoDry supply v2 — cargo (direct online sale) retail revenue records. Idempotent per source SR.</summary>
public interface ICargoDryDirectSaleRepository
{
    Task<CargoDryDirectSaleEntity?> GetBySourceServiceRequestIdAsync(long serviceRequestId, CancellationToken ct = default);
    Task AddAsync(CargoDryDirectSaleEntity entity, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
