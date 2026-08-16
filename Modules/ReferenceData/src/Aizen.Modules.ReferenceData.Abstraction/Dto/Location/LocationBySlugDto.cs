namespace Aizen.Modules.ReferenceData.Abstraction.Dto.Location;

/// <summary>
/// M3 — flat by-slug resolution result: the resolved location plus its ancestor chain (top-down). <c>LocationType</c>
/// is one of <c>country</c> / <c>city</c> / <c>district</c> / <c>neighborhood</c>.
/// </summary>
public sealed class LocationBySlugDto
{
    public string LocationType { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public List<LocationRefDto> ParentChain { get; set; } = new();
}

/// <summary>One ancestor in a location's parent chain — type + code + display name.</summary>
public sealed class LocationRefDto
{
    public string LocationType { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
}
