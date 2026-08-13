namespace Aizen.Modules.Content.Abstraction.Model;

/// <summary>
/// Body for adding or replacing a single-language translation of a content item
/// (§8 UpsertContentTranslation). The item is identified by route; Lang keys the translation.
/// </summary>
public sealed class UpsertContentTranslationRequest
{
    public string Lang { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Summary { get; set; }
    public string? Body { get; set; }
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
}
