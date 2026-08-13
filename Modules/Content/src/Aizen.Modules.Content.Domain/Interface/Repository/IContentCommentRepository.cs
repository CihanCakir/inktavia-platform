using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.Content.Domain.MongoDocuments;

namespace Aizen.Modules.Content.Domain.Interface.Repository;

/// <summary>
/// Persistence surface for content_comments. Implemented in the Repository layer (Phase C2).
/// </summary>
public interface IContentCommentRepository
{
    Task AddAsync(ContentCommentDocument document, CancellationToken ct = default);
    Task ReplaceAsync(ContentCommentDocument document, CancellationToken ct = default);

    Task<ContentCommentDocument?> GetByIdAsync(string id, CancellationToken ct = default);

    Task<IReadOnlyList<ContentCommentDocument>> GetByContentAsync(
        string contentId,
        ContentCommentStatus? status = null,
        int skip = 0,
        int take = 20,
        CancellationToken ct = default);

    Task<long> CountByContentAsync(
        string contentId,
        ContentCommentStatus? status = null,
        CancellationToken ct = default);

    /// <summary>Most recent comment by an author on a content item (for "my comment status").</summary>
    Task<ContentCommentDocument?> GetLatestByAuthorAsync(
        string contentId,
        long authorUserId,
        CancellationToken ct = default);

    /// <summary>All of an author's own comments on a content item, any status, newest first.</summary>
    Task<IReadOnlyList<ContentCommentDocument>> GetByContentAndAuthorAsync(
        string contentId,
        long authorUserId,
        CancellationToken ct = default);

    /// <summary>Soft-delete: set IsDeleted = true and persist.</summary>
    Task SoftDeleteAsync(string id, CancellationToken ct = default);
}
