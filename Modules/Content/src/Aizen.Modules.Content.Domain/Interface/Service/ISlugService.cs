namespace Aizen.Modules.Content.Domain.Interface.Service;

/// <summary>
/// Domain service that produces URL-safe, unique slugs for content items and categories.
/// Implemented in the Application layer (Phase C4).
/// </summary>
public interface ISlugService
{
    /// <summary>Normalize arbitrary text into a URL-safe slug (lower-case, hyphenated, ASCII).</summary>
    string Slugify(string value);

    /// <summary>
    /// Produce a slug from the given text (or an explicit desired slug) that is not yet used by a
    /// non-deleted content item, appending a numeric suffix on collision.
    /// </summary>
    Task<string> GenerateUniqueSlugAsync(string desiredSlugOrTitle, CancellationToken ct = default);
}
