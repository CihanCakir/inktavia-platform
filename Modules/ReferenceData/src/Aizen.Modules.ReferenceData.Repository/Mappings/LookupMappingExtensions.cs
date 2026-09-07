using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupGroup;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem;
using Aizen.Modules.ReferenceData.Domain.Entities.LookupGroup;

namespace Aizen.Modules.ReferenceData.Repository.Mappings;

public static class LookupMappingExtensions
{
    public static LookupGroupDto ToDto(this LookupGroupEntity entity) => new()
    {
        Id = entity.Id,
        ParentLookupGroupId = entity.ParentLookupGroupId,
        Code = entity.Code,
        Name = entity.Name,
        Description = entity.Description,
        GroupType = entity.GroupType,
        Level = entity.Level,
        HierarchyPath = entity.HierarchyPath,
        SortOrder = entity.SortOrder,
        IsSystemGroup = entity.IsSystemGroup,
        IsActive = entity.IsActive
    };

    public static LookupGroupTreeDto ToTreeDto(this LookupGroupEntity entity) => new()
    {
        Id = entity.Id,
        ParentLookupGroupId = entity.ParentLookupGroupId,
        Code = entity.Code,
        Name = entity.Name,
        Description = entity.Description,
        GroupType = entity.GroupType,
        Level = entity.Level,
        HierarchyPath = entity.HierarchyPath,
        SortOrder = entity.SortOrder,
        IsSystemGroup = entity.IsSystemGroup,
        IsActive = entity.IsActive,
        Items = entity.Items.Select(i => i.ToDto()).ToList()
    };

    public static LookupItemDto ToDto(this LookupItemEntity entity) => new()
    {
        Id = entity.Id,
        LookupGroupId = entity.LookupGroupId,
        GroupCode = entity.LookupGroup?.Code ?? string.Empty,
        GroupName = entity.LookupGroup?.Name,
        GroupType = entity.LookupGroup?.GroupType ?? default,
        GroupHierarchyPath = entity.LookupGroup?.HierarchyPath,
        Code = entity.Code,
        Name = entity.Name,
        DisplayNameTr = entity.DisplayNameTr,
        Description = entity.Description,
        IconKey = entity.IconKey,
        ColorCode = entity.ColorCode,
        SortOrder = entity.SortOrder,
        IsDefault = entity.IsDefault,
        IsActive = entity.IsActive
    };
}
