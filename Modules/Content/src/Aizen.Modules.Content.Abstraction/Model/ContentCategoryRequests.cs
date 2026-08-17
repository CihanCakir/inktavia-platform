namespace Aizen.Modules.Content.Abstraction.Model;

/// <summary>Body for creating an editorial category (§8 CreateContentCategory).</summary>
public sealed class CreateContentCategoryRequest
{
    public string Slug { get; set; } = default!;

    /// <summary>Localized display names keyed by language code (e.g. "tr", "en").</summary>
    public Dictionary<string, string> Name { get; set; } = new();

    public string? ParentSlug { get; set; }
    public int Position { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Body for updating an editorial category (§8 UpdateContentCategory). Slug is immutable.</summary>
public sealed class UpdateContentCategoryRequest
{
    public Dictionary<string, string> Name { get; set; } = new();
    public string? ParentSlug { get; set; }
    public int Position { get; set; }
    public bool IsActive { get; set; } = true;
}
