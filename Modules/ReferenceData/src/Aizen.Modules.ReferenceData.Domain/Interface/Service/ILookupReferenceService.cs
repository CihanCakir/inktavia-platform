using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupGroup;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem;
using Aizen.Modules.ReferenceData.Abstraction.Request.LookupGroup;
using Aizen.Modules.ReferenceData.Abstraction.Request.LookupItem;

namespace Aizen.Modules.ReferenceData.Domain.Interface.Service;

public interface ILookupReferenceService
{
    Task<LookupGroupDto> CreateGroupAsync(CreateLookupGroupRequest request, CancellationToken cancellationToken = default);
    Task<LookupGroupDto> UpdateGroupAsync(long id, UpdateLookupGroupRequest request, CancellationToken cancellationToken = default);
    Task ActivateGroupAsync(long id, CancellationToken cancellationToken = default);
    Task DeactivateGroupAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupGroupDto>> GetGroupsAsync(bool onlyActive, CancellationToken cancellationToken = default);
    Task<LookupGroupDto?> GetGroupDetailAsync(long id, CancellationToken cancellationToken = default);

    Task<LookupItemDto> CreateItemAsync(CreateLookupItemRequest request, CancellationToken cancellationToken = default);
    Task<LookupItemDto> UpdateItemAsync(long id, string name, string? description, string? iconKey, string? colorCode, int sortOrder, bool isDefault, bool isActive, CancellationToken cancellationToken = default);
    Task ActivateItemAsync(long id, CancellationToken cancellationToken = default);
    Task DeactivateItemAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupItemDto>> GetItemsByGroupCodeAsync(string groupCode, bool onlyActive, CancellationToken cancellationToken = default);
}
