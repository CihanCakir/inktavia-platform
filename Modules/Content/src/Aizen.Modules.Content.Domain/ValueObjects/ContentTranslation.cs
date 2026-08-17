namespace Aizen.Modules.Content.Domain.ValueObjects;

/// <summary>
/// A single-language rendition of a content item, keyed by language code (§5.1).
/// Language codes are owned by ReferenceData (B4); Content only stores the code.
/// </summary>
public sealed class ContentTranslation
{
    /// <summary>ISO language code, e.g. "tr" / "en".</summary>
    public string Lang { get; set; } = default!;

    public string Title { get; set; } = default!;
    public string? Summary { get; set; }

    /// <summary>Rich body — markdown / HTML.</summary>
    public string? Body { get; set; }

    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
}
