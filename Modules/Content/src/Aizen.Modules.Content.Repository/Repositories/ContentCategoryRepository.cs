using Aizen.Core.Data.Mongo;
using Aizen.Core.Data.Mongo.Repository;
using Aizen.Modules.Content.Domain.Interface.Repository;
using Aizen.Modules.Content.Domain.MongoDocuments;
using Aizen.Modules.Content.Repository.Persistence;
using MongoDB.Driver.Linq;

namespace Aizen.Modules.Content.Repository.Repositories;

[DocumentationInfo("Content category repository",
    "MongoDB repository for content_categories (editorial taxonomy). Reads exclude soft-deleted " +
    "documents; deletes are soft (IsDeleted = true + ReplaceAsync).")]
public sealed class ContentCategoryRepository : IContentCategoryRepository
{
    private readonly IAizenMongoRepository<ContentCategoryDocument> _categories;

    public ContentCategoryRepository(IAizenMongoRepositoryFactory<ContentMongoDbContext> factory)
    {
        _categories = factory.GetRepository<ContentCategoryDocument>();
    }

    public Task AddAsync(ContentCategoryDocument document, CancellationToken ct = default)
        => _categories.AddAsync(document, ct);

    public Task ReplaceAsync(ContentCategoryDocument document, CancellationToken ct = default)
        => _categories.ReplaceAsync(document, ct);

    public Task<ContentCategoryDocument?> GetBySlugAsync(string slug, CancellationToken ct = default)
        => _categories.FindAsync(x => x.Slug == slug, cancellationToken: ct)!;

    // CreateQuery applies the IsDeleted global filter; AnyAsync on the raw collection would not.
    public Task<bool> SlugExistsAsync(string slug, string? excludeId = null, CancellationToken ct = default)
        => string.IsNullOrEmpty(excludeId)
            ? _categories.CreateQuery(x => x.Slug == slug).AnyAsync(ct)
            : _categories.CreateQuery(x => x.Slug == slug && x.Id != excludeId).AnyAsync(ct);

    public async Task<IReadOnlyList<ContentCategoryDocument>> GetAllAsync(
        bool activeOnly = false,
        CancellationToken ct = default)
    {
        var result = await _categories.FindManyAsync(
            predicate: activeOnly ? x => x.IsActive : null,
            orderBy: q => (IOrderedMongoQueryable<ContentCategoryDocument>)
                q.OrderBy(x => x.Position).ThenBy(x => x.Slug),
            topCount: -1,
            cancellationToken: ct);

        return (IReadOnlyList<ContentCategoryDocument>)result;
    }

    public async Task SoftDeleteAsync(string id, CancellationToken ct = default)
    {
        var existing = await _categories.FindAsync(x => x.Id == id, cancellationToken: ct);
        if (existing is null)
            return; // idempotent

        existing.IsDeleted = true;
        await _categories.ReplaceAsync(existing, ct);
    }
}
