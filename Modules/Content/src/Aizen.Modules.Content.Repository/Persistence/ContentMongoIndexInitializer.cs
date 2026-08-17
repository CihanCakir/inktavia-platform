using Aizen.Modules.Content.Domain.MongoDocuments;
using MongoDB.Driver;

namespace Aizen.Modules.Content.Repository.Persistence;

[DocumentationInfo("Content MongoDB index initializer",
    "Ensures required indexes on the Content collections (§5). Runs on host startup. " +
    "Unique constraints that interact with soft-delete use partialFilterExpression { isDeleted: false } " +
    "so a soft-deleted document does not block reuse of its slug or a re-favorite.")]
public sealed class ContentMongoIndexInitializer
{
    private readonly IMongoDatabase _db;

    public ContentMongoIndexInitializer(ContentMongoDbContext context)
    {
        _db = context.Database;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await CreateItemIndexesAsync(ct);
        await CreateCommentIndexesAsync(ct);
        await CreateFavoriteIndexesAsync(ct);
        await CreateCategoryIndexesAsync(ct);
    }

    private async Task CreateItemIndexesAsync(CancellationToken ct)
    {
        var col = _db.GetCollection<ContentItemDocument>(ContentMongoCollectionNames.Items);

        // Unique slug — scoped to live documents so a soft-deleted item releases its slug.
        await col.Indexes.CreateOneAsync(new CreateIndexModel<ContentItemDocument>(
            Builders<ContentItemDocument>.IndexKeys.Ascending(x => x.Slug),
            new CreateIndexOptions<ContentItemDocument>
            {
                Unique = true,
                Name = "ux_content_slug",
                PartialFilterExpression = Builders<ContentItemDocument>.Filter.Eq(x => x.IsDeleted, false)
            }), cancellationToken: ct);

        await col.Indexes.CreateOneAsync(new CreateIndexModel<ContentItemDocument>(
            Builders<ContentItemDocument>.IndexKeys.Ascending(x => x.Status),
            new CreateIndexOptions { Name = "ix_content_status" }), cancellationToken: ct);

        await col.Indexes.CreateOneAsync(new CreateIndexModel<ContentItemDocument>(
            Builders<ContentItemDocument>.IndexKeys.Ascending(x => x.Type),
            new CreateIndexOptions { Name = "ix_content_type" }), cancellationToken: ct);

        // Multikey over the embedded placements array (field path is PascalCase — no camelCase convention).
        await col.Indexes.CreateOneAsync(new CreateIndexModel<ContentItemDocument>(
            Builders<ContentItemDocument>.IndexKeys.Ascending("Placements.Surface"),
            new CreateIndexOptions { Name = "ix_content_placement_surface" }), cancellationToken: ct);

        // PublishAt / ExpireAt are DateTimeOffset, which the MongoDB driver serializes as a BSON array
        // ([ticks, offset]). A COMPOUND index over two such fields is rejected ("cannot index parallel
        // arrays"), so they are indexed separately. (Publish-window filtering itself is done in memory
        // in the read handlers, since DateTimeOffset range predicates do not translate server-side.)
        await DropIndexIfExistsAsync(col, "ix_content_publish_expire", ct);
        await col.Indexes.CreateOneAsync(new CreateIndexModel<ContentItemDocument>(
            Builders<ContentItemDocument>.IndexKeys.Ascending(x => x.PublishAt),
            new CreateIndexOptions { Name = "ix_content_publish_at" }), cancellationToken: ct);
        await col.Indexes.CreateOneAsync(new CreateIndexModel<ContentItemDocument>(
            Builders<ContentItemDocument>.IndexKeys.Ascending(x => x.ExpireAt),
            new CreateIndexOptions { Name = "ix_content_expire_at" }), cancellationToken: ct);

        await col.Indexes.CreateOneAsync(new CreateIndexModel<ContentItemDocument>(
            Builders<ContentItemDocument>.IndexKeys.Ascending("Audience.Type"),
            new CreateIndexOptions { Name = "ix_content_audience_type" }), cancellationToken: ct);

        await col.Indexes.CreateOneAsync(new CreateIndexModel<ContentItemDocument>(
            Builders<ContentItemDocument>.IndexKeys.Ascending(x => x.Tags),
            new CreateIndexOptions { Name = "ix_content_tags" }), cancellationToken: ct);
    }

    private async Task CreateCommentIndexesAsync(CancellationToken ct)
    {
        var col = _db.GetCollection<ContentCommentDocument>(ContentMongoCollectionNames.Comments);

        await col.Indexes.CreateOneAsync(new CreateIndexModel<ContentCommentDocument>(
            Builders<ContentCommentDocument>.IndexKeys
                .Ascending(x => x.ContentId)
                .Ascending(x => x.Status)
                .Ascending(x => x.CreatedAt),
            new CreateIndexOptions { Name = "ix_comment_content_status_created" }), cancellationToken: ct);

        await col.Indexes.CreateOneAsync(new CreateIndexModel<ContentCommentDocument>(
            Builders<ContentCommentDocument>.IndexKeys.Ascending(x => x.AuthorUserId),
            new CreateIndexOptions { Name = "ix_comment_author" }), cancellationToken: ct);
    }

    private async Task CreateFavoriteIndexesAsync(CancellationToken ct)
    {
        var col = _db.GetCollection<ContentFavoriteDocument>(ContentMongoCollectionNames.Favorites);

        // One live favorite per (content, user) — scoped to live documents so a removed favorite
        // can be re-added without violating the unique constraint.
        await col.Indexes.CreateOneAsync(new CreateIndexModel<ContentFavoriteDocument>(
            Builders<ContentFavoriteDocument>.IndexKeys
                .Ascending(x => x.ContentId)
                .Ascending(x => x.UserId),
            new CreateIndexOptions<ContentFavoriteDocument>
            {
                Unique = true,
                Name = "ux_favorite_content_user",
                PartialFilterExpression = Builders<ContentFavoriteDocument>.Filter.Eq(x => x.IsDeleted, false)
            }), cancellationToken: ct);

        await col.Indexes.CreateOneAsync(new CreateIndexModel<ContentFavoriteDocument>(
            Builders<ContentFavoriteDocument>.IndexKeys
                .Ascending(x => x.UserId)
                .Descending(x => x.CreatedAt),
            new CreateIndexOptions { Name = "ix_favorite_user_created" }), cancellationToken: ct);
    }

    private static async Task DropIndexIfExistsAsync<T>(IMongoCollection<T> col, string name, CancellationToken ct)
    {
        using var cursor = await col.Indexes.ListAsync(ct);
        var existing = await cursor.ToListAsync(ct);
        if (existing.Any(ix => ix.TryGetValue("name", out var n) && n.AsString == name))
            await col.Indexes.DropOneAsync(name, ct);
    }

    private Task CreateCategoryIndexesAsync(CancellationToken ct)
    {
        var col = _db.GetCollection<ContentCategoryDocument>(ContentMongoCollectionNames.Categories);

        return col.Indexes.CreateOneAsync(new CreateIndexModel<ContentCategoryDocument>(
            Builders<ContentCategoryDocument>.IndexKeys.Ascending(x => x.Slug),
            new CreateIndexOptions<ContentCategoryDocument>
            {
                Unique = true,
                Name = "ux_category_slug",
                PartialFilterExpression = Builders<ContentCategoryDocument>.Filter.Eq(x => x.IsDeleted, false)
            }), cancellationToken: ct);
    }
}
