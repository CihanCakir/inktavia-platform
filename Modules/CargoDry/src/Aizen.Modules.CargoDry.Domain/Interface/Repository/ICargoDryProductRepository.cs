using Aizen.Modules.CargoDry.Domain.Entities;

namespace Aizen.Modules.CargoDry.Domain.Interface.Repository;

public interface ICargoDryProductRepository
{
    Task<CargoDryProductEntity?> GetByCodeAsync(string productCode, CancellationToken ct = default);
    Task<List<CargoDryProductEntity>> GetAllActiveAsync(CancellationToken ct = default);
    Task AddAsync(CargoDryProductEntity entity, CancellationToken ct = default);
}
