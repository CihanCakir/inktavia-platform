using Aizen.Core.Data.Mongo.Attributes;
using Aizen.Core.Data.Mongo.Document;

namespace Aizen.Modules.Content.Domain.MongoDocuments;

/// <summary>
/// Editorial taxonomy node (§5.4). Content-owned (B7); grows and changes editorially. Small collection.
/// Stored in MongoDB collection: content_categories.
/// </summary>
[AizenCollectionInfo(CollectionName = "content_categories")]
public sealed class ContentCategoryDocument : AizenDocumentBase
{
    /// <summary>Unique, URL-safe identifier for the category.</summary>
    public string Slug { get; set; } = default!;

    /// <summary>Localized display names keyed by language code (e.g. "tr", "en").</summary>
    public Dictionary<string, string> Name { get; set; } = new();

    public string? ParentSlug { get; set; }
    public int Position { get; set; }
    public bool IsActive { get; set; } = true;
}
