using Aizen.Modules.CargoDry.Domain.Entities;

namespace Aizen.Modules.CargoDry.Domain.Interface.Repository;

public interface ICargoDryBatchRepository
{
    Task<CargoDryBatchEntity?> GetByCodeAsync(string batchCode, CancellationToken ct = default);
    Task<List<CargoDryBatchEntity>> GetAllAsync(CancellationToken ct = default);
    Task<List<CargoDryBatchEntity>> GetAllForReportAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
    Task AddAsync(CargoDryBatchEntity entity, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
