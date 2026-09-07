using Aizen.Modules.ReferenceData.Repository.Mappings;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupGroup;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem;
using Aizen.Modules.ReferenceData.Abstraction.Request.LookupGroup;
using Aizen.Modules.ReferenceData.Abstraction.Request.LookupItem;
using Aizen.Modules.ReferenceData.Domain.Entities.LookupGroup;
using Aizen.Modules.ReferenceData.Domain.Interface;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;
using Aizen.Modules.ReferenceData.Repository.Context;

namespace Aizen.Modules.ReferenceData.Repository.Service;

public sealed class LookupReferenceService : ILookupReferenceService
{
    private readonly ILookupGroupRepository _groupRepo;
    private readonly ILookupItemRepository _itemRepo;
    private readonly ReferenceDataDbContext _dbContext;

    public LookupReferenceService(ILookupGroupRepository groupRepo, ILookupItemRepository itemRepo, ReferenceDataDbContext dbContext)
    {
        _groupRepo = groupRepo;
        _itemRepo = itemRepo;
        _dbContext = dbContext;
    }

    public async Task<LookupGroupDto> CreateGroupAsync(CreateLookupGroupRequest request, CancellationToken cancellationToken = default)
    {
        var exists = await _groupRepo.ExistsByCodeAsync(request.Code, cancellationToken);
        if (exists) throw new AizenBusinessException($"Lookup group with code '{request.Code}' already exists.");

        LookupGroupEntity entity;
        if (request.ParentLookupGroupId.HasValue)
        {
            var parent = await _groupRepo.GetGroupByIdAsync(request.ParentLookupGroupId.Value, cancellationToken)
                ?? throw new AizenBusinessException($"Parent lookup group '{request.ParentLookupGroupId}' not found.");
            entity = LookupGroupEntity.CreateChild(parent.Id, parent.HierarchyPath, parent.Level, request.Code, request.Name, request.Description, request.GroupType, request.IsSystemGroup, request.SortOrder);
        }
        else
        {
            entity = LookupGroupEntity.CreateRoot(request.Code, request.Name, request.Description, request.GroupType, request.IsSystemGroup, request.SortOrder);
        }

        await _groupRepo.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToDto();
    }

    public async Task<LookupGroupDto> UpdateGroupAsync(long id, UpdateLookupGroupRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _groupRepo.GetGroupByIdAsync(id, cancellationToken)
            ?? throw new AizenBusinessException($"Lookup group with id '{id}' not found.");
        entity.Update(request.Name, request.Description, request.GroupType, request.SortOrder, request.IsActive);
        _groupRepo.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToDto();
    }

    public async Task ActivateGroupAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _groupRepo.GetGroupByIdAsync(id, cancellationToken)
            ?? throw new AizenBusinessException($"Lookup group with id '{id}' not found.");
        entity.Activate();
        _groupRepo.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateGroupAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _groupRepo.GetGroupByIdAsync(id, cancellationToken)
            ?? throw new AizenBusinessException($"Lookup group with id '{id}' not found.");
        entity.Deactivate();
        _groupRepo.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LookupGroupDto>> GetGroupsAsync(bool onlyActive, CancellationToken cancellationToken = default)
    {
        var list = await _groupRepo.GetGroupsAsync(onlyActive, cancellationToken);
        return list.Select(x => x.ToDto()).ToList();
    }

    public async Task<LookupGroupDto?> GetGroupDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _groupRepo.GetGroupByIdAsync(id, cancellationToken);
        return entity?.ToDto();
    }

    public async Task<LookupItemDto> CreateItemAsync(CreateLookupItemRequest request, CancellationToken cancellationToken = default)
    {
        var group = await _groupRepo.GetGroupByCodeAsync(request.GroupCode, cancellationToken)
            ?? throw new AizenBusinessException($"Lookup group with code '{request.GroupCode}' not found.");
        var exists = await _itemRepo.ExistsByCodeInGroupAsync(group.Id, request.Code, cancellationToken);
        if (exists) throw new AizenBusinessException($"Lookup item with code '{request.Code}' already exists in group '{request.GroupCode}'.");

        var entity = LookupItemEntity.Create(group.Id, request.Code, request.Name, request.Description, request.IconKey, request.ColorCode, request.SortOrder, request.IsDefault);
        await _itemRepo.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var reloaded = await _itemRepo.GetItemByIdAsync(entity.Id, cancellationToken) ?? entity;
        return reloaded.ToDto();
    }

    public async Task<LookupItemDto> UpdateItemAsync(long id, string name, string? description, string? iconKey, string? colorCode, int sortOrder, bool isDefault, bool isActive, string? displayNameTr = null, CancellationToken cancellationToken = default)
    {
        var entity = await _itemRepo.GetItemByIdAsync(id, cancellationToken)
            ?? throw new AizenBusinessException($"Lookup item with id '{id}' not found.");
        entity.Update(name, description, iconKey, colorCode, sortOrder, isDefault, isActive, displayNameTr);
        _itemRepo.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToDto();
    }

    public async Task ActivateItemAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _itemRepo.GetItemByIdAsync(id, cancellationToken)
            ?? throw new AizenBusinessException($"Lookup item with id '{id}' not found.");
        entity.Activate();
        _itemRepo.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateItemAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _itemRepo.GetItemByIdAsync(id, cancellationToken)
            ?? throw new AizenBusinessException($"Lookup item with id '{id}' not found.");
        entity.Deactivate();
        _itemRepo.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LookupItemDto>> GetItemsByGroupCodeAsync(string groupCode, bool onlyActive, CancellationToken cancellationToken = default)
    {
        var list = await _itemRepo.GetItemsByGroupCodeAsync(groupCode, onlyActive, cancellationToken);
        return list.Select(x => x.ToDto()).ToList();
    }
}
