using Aizen.Modules.ReferenceData.Domain.Entities.LookupGroup;

namespace Aizen.Modules.ReferenceData.Domain.Interface;

public interface ILookupGroupRepository
{
    Task<LookupGroupEntity?> GetGroupByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<LookupGroupEntity?> GetGroupByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupGroupEntity>> GetGroupsAsync(bool onlyActive, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupGroupEntity>> GetGroupTreeAsync(bool onlyActive, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupGroupEntity>> GetChildrenAsync(long parentGroupId, bool onlyActive, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupGroupEntity>> GetDescendantsAsync(string hierarchyPath, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task AddAsync(LookupGroupEntity entity, CancellationToken cancellationToken = default);
    void Update(LookupGroupEntity entity);
}
