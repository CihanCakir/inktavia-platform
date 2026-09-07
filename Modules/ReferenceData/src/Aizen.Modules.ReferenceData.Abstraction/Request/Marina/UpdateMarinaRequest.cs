namespace Aizen.Modules.ReferenceData.Abstraction.Request.Marina;

/// <summary>Admin curation edit of a marina's display fields (name + linked city code). Matches admin-web payload.</summary>
public sealed class UpdateMarinaRequest
{
    public string Name { get; set; } = default!;
    public string? CityCode { get; set; }
}
