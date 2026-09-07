using Aizen.Core.Domain;

namespace Aizen.Modules.ReferenceData.Domain.Entities.LookupGroup;

public sealed class LookupItemEntity : AizenEntityWithAudit
{
    public long LookupGroupId { get; private set; }
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    /// <summary>Turkish display name (additive; null → clients fall back to <see cref="Name"/>). English name stays in Name.</summary>
    public string? DisplayNameTr { get; private set; }
    public string? Description { get; private set; }
    public string? IconKey { get; private set; }
    public string? ColorCode { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsDefault { get; private set; }

    public LookupGroupEntity LookupGroup { get; private set; } = default!;

    public LookupItemEntity() { }

    public static LookupItemEntity Create(long lookupGroupId, string code, string name, string? description, string? iconKey, string? colorCode, int sortOrder, bool isDefault, string? displayNameTr = null)
    {
        return new LookupItemEntity
        {
            LookupGroupId = lookupGroupId,
            Code = NormalizeCode(code),
            Name = name.Trim(),
            DisplayNameTr = string.IsNullOrWhiteSpace(displayNameTr) ? null : displayNameTr.Trim(),
            Description = description,
            IconKey = iconKey,
            ColorCode = colorCode,
            SortOrder = sortOrder,
            IsDefault = isDefault,
            IsActive = true
        };
    }

    public void Update(string name, string? description, string? iconKey, string? colorCode, int sortOrder, bool isDefault, bool isActive, string? displayNameTr = null)
    {
        Name = name.Trim();
        DisplayNameTr = string.IsNullOrWhiteSpace(displayNameTr) ? null : displayNameTr.Trim();
        Description = description;
        IconKey = iconKey;
        ColorCode = colorCode;
        SortOrder = sortOrder;
        IsDefault = isDefault;
        IsActive = isActive;
    }

    public void ChangeSortOrder(int sortOrder) => SortOrder = sortOrder;
    public void SetDefault() => IsDefault = true;
    public void UnsetDefault() => IsDefault = false;
    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Lookup item code is required.", nameof(code));

        return code.Trim().ToUpperInvariant();
    }
}
