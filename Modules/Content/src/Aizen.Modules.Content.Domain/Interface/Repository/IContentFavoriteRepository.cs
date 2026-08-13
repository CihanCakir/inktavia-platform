using Aizen.Modules.Content.Domain.MongoDocuments;

namespace Aizen.Modules.Content.Domain.Interface.Repository;

/// <summary>
/// Persistence surface for content_favorites (one per user+content). Implemented in Phase C2.
/// </summary>
public interface IContentFavoriteRepository
{
    Task AddAsync(ContentFavoriteDocument document, CancellationToken ct = default);

    /// <summary>
    /// Idempotently add a favorite. Returns true when a new favorite row was inserted, false when the
    /// user had already favorited the item (checked first; a racing duplicate-key insert is also treated
    /// as "already favorited"). A prior soft-deleted favorite does not block re-adding (partial index).
    /// </summary>
    Task<bool> TryAddAsync(ContentFavoriteDocument document, CancellationToken ct = default);

    /// <summary>True when the user currently favorites the content item (excludes soft-deleted).</summary>
    Task<bool> ExistsAsync(string contentId, long userId, CancellationToken ct = default);

    /// <summary>Soft-delete the user's favorite of a content item. Returns true when one was removed.</summary>
    Task<bool> RemoveAsync(string contentId, long userId, CancellationToken ct = default);

    Task<IReadOnlyList<ContentFavoriteDocument>> GetByUserAsync(
        long userId,
        int skip = 0,
        int take = 20,
        CancellationToken ct = default);

    Task<long> CountByUserAsync(long userId, CancellationToken ct = default);
}
