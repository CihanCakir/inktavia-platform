namespace Aizen.Modules.Content.Abstraction.Dto;

/// <summary>Read model of an editorial taxonomy node (§5.4).</summary>
public sealed class ContentCategoryDto
{
    public string Id { get; set; } = default!;
    public string Slug { get; set; } = default!;

    /// <summary>Localized display names keyed by language code (e.g. "tr", "en").</summary>
    public Dictionary<string, string> Name { get; set; } = new();

    public string? ParentSlug { get; set; }
    public int Position { get; set; }
    public bool IsActive { get; set; }
}
