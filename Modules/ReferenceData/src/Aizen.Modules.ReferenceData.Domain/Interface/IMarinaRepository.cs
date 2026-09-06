using Aizen.Modules.ReferenceData.Domain.Entities.Marina;

namespace Aizen.Modules.ReferenceData.Domain.Interface;

public interface IMarinaRepository
{
    Task<MarinaEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<MarinaEntity?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task AddAsync(MarinaEntity entity, CancellationToken cancellationToken = default);
    void Update(MarinaEntity entity);
}
