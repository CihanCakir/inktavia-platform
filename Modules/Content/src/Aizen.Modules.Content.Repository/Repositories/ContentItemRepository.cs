using System.Linq.Expressions;
using Aizen.Core.Data.Mongo;
using Aizen.Core.Data.Mongo.Repository;
using Aizen.Modules.Content.Domain.Interface.Repository;
using Aizen.Modules.Content.Domain.MongoDocuments;
using Aizen.Modules.Content.Repository.Persistence;
using MongoDB.Driver.Linq;

namespace Aizen.Modules.Content.Repository.Repositories;

[DocumentationInfo("Content item repository",
    "MongoDB repository for the content_items aggregate root. Reads exclude soft-deleted documents " +
    "via the AizenDocumentBase global filter; deletes are soft (IsDeleted = true + ReplaceAsync).")]
public sealed class ContentItemRepository : IContentItemRepository
{
    private readonly IAizenMongoRepository<ContentItemDocument> _items;

    public ContentItemRepository(IAizenMongoRepositoryFactory<ContentMongoDbContext> factory)
    {
        _items = factory.GetRepository<ContentItemDocument>();
    }

    public Task AddAsync(ContentItemDocument document, CancellationToken ct = default)
        => _items.AddAsync(document, ct);

    public Task ReplaceAsync(ContentItemDocument document, CancellationToken ct = default)
        => _items.ReplaceAsync(document, ct);

    public Task<ContentItemDocument?> GetByIdAsync(string id, CancellationToken ct = default)
        => _items.FindAsync(x => x.Id == id, cancellationToken: ct)!;

    public Task<ContentItemDocument?> GetBySlugAsync(string slug, CancellationToken ct = default)
        => _items.FindAsync(x => x.Slug == slug, cancellationToken: ct)!;

    // NOTE: AnyAsync/CountAsync on the Aizen Mongo repo query the raw collection and do NOT apply the
    // IsDeleted global filter (only Find* do). Existence/count reads therefore go through CreateQuery,
    // which applies the filter — so a soft-deleted slug is correctly reported as free.
    public Task<bool> SlugExistsAsync(string slug, string? excludeId = null, CancellationToken ct = default)
        => string.IsNullOrEmpty(excludeId)
            ? _items.CreateQuery(x => x.Slug == slug).AnyAsync(ct)
            : _items.CreateQuery(x => x.Slug == slug && x.Id != excludeId).AnyAsync(ct);

    public async Task<IReadOnlyList<ContentItemDocument>> FindManyAsync(
        Expression<Func<ContentItemDocument, bool>> predicate,
        int skip = 0,
        int take = 20,
        CancellationToken ct = default)
    {
        // Fetch the requested page in a stable order, then apply the offset in memory.
        // FindManyAsync routes through CreateQuery, which applies the IsDeleted global filter.
        var page = await _items.FindManyAsync(
            predicate: predicate,
            orderBy: q => (IOrderedMongoQueryable<ContentItemDocument>)
                q.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id),
            topCount: skip + take,
            cancellationToken: ct);

        return page.Skip(skip).ToList();
    }

    public Task<long> CountAsync(
        Expression<Func<ContentItemDocument, bool>> predicate,
        CancellationToken ct = default)
        => _items.CreateQuery(predicate).LongCountAsync(ct);

    public async Task SoftDeleteAsync(string id, CancellationToken ct = default)
    {
        var existing = await _items.FindAsync(x => x.Id == id, cancellationToken: ct);
        if (existing is null)
            return; // already gone / soft-deleted (excluded by the global filter) — idempotent.

        existing.IsDeleted = true;
        existing.UpdatedAt = DateTimeOffset.UtcNow;
        await _items.ReplaceAsync(existing, ct);
    }

    // Native atomic $inc — no read-modify-write, so concurrent engagement stays consistent.
    public Task IncrementFavoriteCountAsync(string id, int delta, CancellationToken ct = default)
        => _items.UpdateAsync(u => u.Inc(x => x.FavoriteCount, delta), x => x.Id == id, ct);

    public Task IncrementCommentCountAsync(string id, int delta, CancellationToken ct = default)
        => _items.UpdateAsync(u => u.Inc(x => x.CommentCount, delta), x => x.Id == id, ct);
}
