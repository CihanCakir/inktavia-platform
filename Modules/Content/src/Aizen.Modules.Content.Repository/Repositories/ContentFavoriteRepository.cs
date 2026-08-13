using Aizen.Core.Data.Mongo;
using Aizen.Core.Data.Mongo.Repository;
using Aizen.Modules.Content.Domain.Interface.Repository;
using Aizen.Modules.Content.Domain.MongoDocuments;
using Aizen.Modules.Content.Repository.Persistence;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Aizen.Modules.Content.Repository.Repositories;

[DocumentationInfo("Content favorite repository",
    "MongoDB repository for content_favorites (one live favorite per user+content). Reads exclude " +
    "soft-deleted documents; removal is soft (IsDeleted = true + ReplaceAsync).")]
public sealed class ContentFavoriteRepository : IContentFavoriteRepository
{
    private readonly IAizenMongoRepository<ContentFavoriteDocument> _favorites;

    public ContentFavoriteRepository(IAizenMongoRepositoryFactory<ContentMongoDbContext> factory)
    {
        _favorites = factory.GetRepository<ContentFavoriteDocument>();
    }

    public Task AddAsync(ContentFavoriteDocument document, CancellationToken ct = default)
        => _favorites.AddAsync(document, ct);

    public async Task<bool> TryAddAsync(ContentFavoriteDocument document, CancellationToken ct = default)
    {
        if (await ExistsAsync(document.ContentId, document.UserId, ct))
            return false;

        try
        {
            await _favorites.AddAsync(document, ct);
            return true;
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            // Lost a race with a concurrent add — the unique partial index rejected the duplicate.
            return false;
        }
    }

    // CreateQuery applies the IsDeleted global filter; AnyAsync on the raw collection would not — so a
    // soft-deleted (removed) favorite is correctly reported as absent and can be re-added.
    public Task<bool> ExistsAsync(string contentId, long userId, CancellationToken ct = default)
        => _favorites.CreateQuery(x => x.ContentId == contentId && x.UserId == userId).AnyAsync(ct);

    public async Task<bool> RemoveAsync(string contentId, long userId, CancellationToken ct = default)
    {
        var existing = await _favorites.FindAsync(
            x => x.ContentId == contentId && x.UserId == userId, cancellationToken: ct);
        if (existing is null)
            return false; // not favorited (or already removed) — idempotent no-op

        existing.IsDeleted = true;
        await _favorites.ReplaceAsync(existing, ct);
        return true;
    }

    public async Task<IReadOnlyList<ContentFavoriteDocument>> GetByUserAsync(
        long userId,
        int skip = 0,
        int take = 20,
        CancellationToken ct = default)
    {
        var page = await _favorites.FindManyAsync(
            predicate: x => x.UserId == userId,
            orderBy: q => (IOrderedMongoQueryable<ContentFavoriteDocument>)
                q.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id),
            topCount: skip + take,
            cancellationToken: ct);

        return page.Skip(skip).ToList();
    }

    public Task<long> CountByUserAsync(long userId, CancellationToken ct = default)
        => _favorites.CreateQuery(x => x.UserId == userId).LongCountAsync(ct);
}
