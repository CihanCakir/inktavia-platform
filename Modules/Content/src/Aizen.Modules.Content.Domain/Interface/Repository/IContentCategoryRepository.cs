using Aizen.Modules.Content.Domain.MongoDocuments;

namespace Aizen.Modules.Content.Domain.Interface.Repository;

/// <summary>
/// Persistence surface for content_categories (editorial taxonomy). Implemented in Phase C2.
/// </summary>
public interface IContentCategoryRepository
{
    Task AddAsync(ContentCategoryDocument document, CancellationToken ct = default);
    Task ReplaceAsync(ContentCategoryDocument document, CancellationToken ct = default);

    Task<ContentCategoryDocument?> GetBySlugAsync(string slug, CancellationToken ct = default);

    /// <summary>True when a non-deleted category already uses the slug, optionally excluding one id.</summary>
    Task<bool> SlugExistsAsync(string slug, string? excludeId = null, CancellationToken ct = default);

    Task<IReadOnlyList<ContentCategoryDocument>> GetAllAsync(
        bool activeOnly = false,
        CancellationToken ct = default);

    /// <summary>Soft-delete: set IsDeleted = true and persist.</summary>
    Task SoftDeleteAsync(string id, CancellationToken ct = default);
}
