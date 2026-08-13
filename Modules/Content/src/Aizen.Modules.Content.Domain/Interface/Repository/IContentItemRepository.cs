using System.Linq.Expressions;
using Aizen.Modules.Content.Domain.MongoDocuments;

namespace Aizen.Modules.Content.Domain.Interface.Repository;

/// <summary>
/// Persistence surface for the content_items aggregate root. Implemented in the Repository layer
/// over IAizenMongoRepositoryFactory&lt;ContentMongoDbContext&gt; (Phase C2).
/// </summary>
public interface IContentItemRepository
{
    Task AddAsync(ContentItemDocument document, CancellationToken ct = default);
    Task ReplaceAsync(ContentItemDocument document, CancellationToken ct = default);

    Task<ContentItemDocument?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<ContentItemDocument?> GetBySlugAsync(string slug, CancellationToken ct = default);

    /// <summary>True when a non-deleted item already uses the slug, optionally excluding one id.</summary>
    Task<bool> SlugExistsAsync(string slug, string? excludeId = null, CancellationToken ct = default);

    Task<IReadOnlyList<ContentItemDocument>> FindManyAsync(
        Expression<Func<ContentItemDocument, bool>> predicate,
        int skip = 0,
        int take = 20,
        CancellationToken ct = default);

    Task<long> CountAsync(
        Expression<Func<ContentItemDocument, bool>> predicate,
        CancellationToken ct = default);

    /// <summary>Soft-delete: set IsDeleted = true and persist.</summary>
    Task SoftDeleteAsync(string id, CancellationToken ct = default);

    /// <summary>Atomically adjust the denormalized FavoriteCount ($inc).</summary>
    Task IncrementFavoriteCountAsync(string id, int delta, CancellationToken ct = default);

    /// <summary>Atomically adjust the denormalized CommentCount ($inc).</summary>
    Task IncrementCommentCountAsync(string id, int delta, CancellationToken ct = default);
}
