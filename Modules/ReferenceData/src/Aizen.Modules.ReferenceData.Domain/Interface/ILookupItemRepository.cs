using Aizen.Modules.ReferenceData.Domain.Entities.LookupGroup;

namespace Aizen.Modules.ReferenceData.Domain.Interface;

public interface ILookupItemRepository
{
    Task<LookupItemEntity?> GetItemByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<LookupItemEntity?> GetItemByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupItemEntity>> GetItemsByGroupIdAsync(long lookupGroupId, bool onlyActive, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupItemEntity>> GetItemsByGroupCodeAsync(string groupCode, bool onlyActive, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCodeInGroupAsync(long lookupGroupId, string code, CancellationToken cancellationToken = default);
    Task AddAsync(LookupItemEntity entity, CancellationToken cancellationToken = default);
    void Update(LookupItemEntity entity);
}
