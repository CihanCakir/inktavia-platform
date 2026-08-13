using System.Linq.Expressions;
using Aizen.Core.Data.Mongo;
using Aizen.Core.Data.Mongo.Repository;
using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.Content.Domain.Interface.Repository;
using Aizen.Modules.Content.Domain.MongoDocuments;
using Aizen.Modules.Content.Repository.Persistence;
using MongoDB.Driver.Linq;

namespace Aizen.Modules.Content.Repository.Repositories;

[DocumentationInfo("Content comment repository",
    "MongoDB repository for content_comments. Reads exclude soft-deleted documents via the global " +
    "filter; deletes are soft (IsDeleted = true + ReplaceAsync).")]
public sealed class ContentCommentRepository : IContentCommentRepository
{
    private readonly IAizenMongoRepository<ContentCommentDocument> _comments;

    public ContentCommentRepository(IAizenMongoRepositoryFactory<ContentMongoDbContext> factory)
    {
        _comments = factory.GetRepository<ContentCommentDocument>();
    }

    public Task AddAsync(ContentCommentDocument document, CancellationToken ct = default)
        => _comments.AddAsync(document, ct);

    public Task ReplaceAsync(ContentCommentDocument document, CancellationToken ct = default)
        => _comments.ReplaceAsync(document, ct);

    public Task<ContentCommentDocument?> GetByIdAsync(string id, CancellationToken ct = default)
        => _comments.FindAsync(x => x.Id == id, cancellationToken: ct)!;

    public async Task<IReadOnlyList<ContentCommentDocument>> GetByContentAsync(
        string contentId,
        ContentCommentStatus? status = null,
        int skip = 0,
        int take = 20,
        CancellationToken ct = default)
    {
        Expression<Func<ContentCommentDocument, bool>> predicate = status.HasValue
            ? c => c.ContentId == contentId && c.Status == status.Value
            : c => c.ContentId == contentId;

        var page = await _comments.FindManyAsync(
            predicate: predicate,
            orderBy: q => (IOrderedMongoQueryable<ContentCommentDocument>)
                q.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id),
            topCount: skip + take,
            cancellationToken: ct);

        return page.Skip(skip).ToList();
    }

    // CreateQuery applies the IsDeleted global filter; CountAsync on the raw collection would not.
    public Task<long> CountByContentAsync(
        string contentId,
        ContentCommentStatus? status = null,
        CancellationToken ct = default)
        => status.HasValue
            ? _comments.CreateQuery(c => c.ContentId == contentId && c.Status == status.Value).LongCountAsync(ct)
            : _comments.CreateQuery(c => c.ContentId == contentId).LongCountAsync(ct);

    public Task<ContentCommentDocument?> GetLatestByAuthorAsync(
        string contentId,
        long authorUserId,
        CancellationToken ct = default)
        => _comments.FindAsync(
            predicate: c => c.ContentId == contentId && c.AuthorUserId == authorUserId,
            orderBy: q => (IOrderedMongoQueryable<ContentCommentDocument>)
                q.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id),
            cancellationToken: ct)!;

    public async Task<IReadOnlyList<ContentCommentDocument>> GetByContentAndAuthorAsync(
        string contentId,
        long authorUserId,
        CancellationToken ct = default)
    {
        var results = await _comments.FindManyAsync(
            predicate: c => c.ContentId == contentId && c.AuthorUserId == authorUserId,
            orderBy: q => (IOrderedMongoQueryable<ContentCommentDocument>)
                q.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id),
            topCount: -1,
            cancellationToken: ct);
        return (IReadOnlyList<ContentCommentDocument>)results;
    }

    public async Task SoftDeleteAsync(string id, CancellationToken ct = default)
    {
        var existing = await _comments.FindAsync(x => x.Id == id, cancellationToken: ct);
        if (existing is null)
            return; // idempotent

        existing.IsDeleted = true;
        existing.UpdatedAt = DateTimeOffset.UtcNow;
        await _comments.ReplaceAsync(existing, ct);
    }
}
