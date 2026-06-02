using Aizen.Modules.ReferenceData.Domain.Entities.SystemParameter;

namespace Aizen.Modules.ReferenceData.Domain.Interface;

public interface ISystemParameterRepository
{
    Task<SystemParameterEntity?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SystemParameterEntity>> GetListAsync(bool onlyActive, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SystemParameterEntity>> GetByPrefixAsync(string prefix, bool onlyActive, CancellationToken cancellationToken = default);
    Task AddAsync(SystemParameterEntity entity, CancellationToken cancellationToken = default);
    void Update(SystemParameterEntity entity);
}
