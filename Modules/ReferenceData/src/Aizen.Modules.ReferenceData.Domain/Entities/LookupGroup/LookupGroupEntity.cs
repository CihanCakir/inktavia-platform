using Aizen.Core.Domain;
using Aizen.Modules.ReferenceData.Abstraction.Enum;

namespace Aizen.Modules.ReferenceData.Domain.Entities.LookupGroup;

public sealed class LookupGroupEntity : AizenEntityWithAudit
{
    public long? ParentLookupGroupId { get; private set; }
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public LookupGroupType GroupType { get; private set; }
    public int Level { get; private set; }
    public string HierarchyPath { get; private set; } = default!;
    public int SortOrder { get; private set; }
    public bool IsSystemGroup { get; private set; }

    public LookupGroupEntity? ParentLookupGroup { get; private set; }

    private readonly List<LookupGroupEntity> _children = new();
    public IReadOnlyCollection<LookupGroupEntity> Children => _children.AsReadOnly();

    private readonly List<LookupItemEntity> _items = new();
    public IReadOnlyCollection<LookupItemEntity> Items => _items.AsReadOnly();

    public LookupGroupEntity() { }

    public static LookupGroupEntity CreateRoot(string code, string name, string? description, LookupGroupType groupType, bool isSystemGroup, int sortOrder)
    {
        var normalizedCode = NormalizeCode(code);

        return new LookupGroupEntity
        {
            ParentLookupGroupId = null,
            Code = normalizedCode,
            Name = name.Trim(),
            Description = description,
            GroupType = groupType,
            Level = 0,
            HierarchyPath = normalizedCode,
            SortOrder = sortOrder,
            IsSystemGroup = isSystemGroup,
            IsActive = true
        };
    }

    public static LookupGroupEntity CreateChild(long parentLookupGroupId, string parentHierarchyPath, int parentLevel, string code, string name, string? description, LookupGroupType groupType, bool isSystemGroup, int sortOrder)
    {
        var normalizedCode = NormalizeCode(code);

        return new LookupGroupEntity
        {
            ParentLookupGroupId = parentLookupGroupId,
            Code = normalizedCode,
            Name = name.Trim(),
            Description = description,
            GroupType = groupType,
            Level = parentLevel + 1,
            HierarchyPath = $"{parentHierarchyPath}/{normalizedCode}",
            SortOrder = sortOrder,
            IsSystemGroup = isSystemGroup,
            IsActive = true
        };
    }

    public void Update(string name, string? description, LookupGroupType groupType, int sortOrder, bool isActive)
    {
        Name = name.Trim();
        Description = description;
        GroupType = groupType;
        SortOrder = sortOrder;
        IsActive = isActive;
    }

    public void MoveToRoot()
    {
        ParentLookupGroupId = null;
        Level = 0;
        HierarchyPath = Code;
    }

    public void MoveToParent(long parentLookupGroupId, string parentHierarchyPath, int parentLevel)
    {
        if (parentLookupGroupId == Id)
            throw new InvalidOperationException("Lookup group cannot be moved under itself.");

        ParentLookupGroupId = parentLookupGroupId;
        Level = parentLevel + 1;
        HierarchyPath = $"{parentHierarchyPath}/{Code}";
    }

    public void RefreshHierarchy(string parentHierarchyPath, int parentLevel)
    {
        Level = parentLevel + 1;
        HierarchyPath = $"{parentHierarchyPath}/{Code}";
    }

    public void ChangeSortOrder(int sortOrder) => SortOrder = sortOrder;
    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Lookup group code is required.", nameof(code));

        return code.Trim().ToUpperInvariant();
    }
}
