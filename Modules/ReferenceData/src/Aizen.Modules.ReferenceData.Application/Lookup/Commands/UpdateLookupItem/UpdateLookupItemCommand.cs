using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem;

namespace Aizen.Modules.ReferenceData.Application.Lookup.Commands;

public sealed class UpdateLookupItemCommand : AizenCommand<LookupItemDto>
{
    public long Id { get; }
    public string Name { get; }
    public string? Description { get; }
    public string? IconKey { get; }
    public string? ColorCode { get; }
    public int SortOrder { get; }
    public bool IsDefault { get; }
    public bool IsActive { get; }

    public UpdateLookupItemCommand(long id, string name, string? description, string? iconKey, string? colorCode, int sortOrder, bool isDefault, bool isActive)
    {
        Id = id;
        Name = name;
        Description = description;
        IconKey = iconKey;
        ColorCode = colorCode;
        SortOrder = sortOrder;
        IsDefault = isDefault;
        IsActive = isActive;
    }
}
