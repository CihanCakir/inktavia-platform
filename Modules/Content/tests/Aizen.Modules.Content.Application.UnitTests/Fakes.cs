using System.Linq.Expressions;
using Aizen.Core.Cache.Abstraction;
using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.Content.Application.Services;
using Aizen.Modules.Content.Domain.Interface.Repository;
using Aizen.Modules.Content.Domain.MongoDocuments;
using Aizen.Modules.FileStorage.Abstraction.Dto.Access;
using Aizen.Modules.FileStorage.Abstraction.Dto.File;
using Aizen.Modules.FileStorage.Abstraction.RemoteCall.File;
using Aizen.Modules.FileStorage.Abstraction.RemoteCall.File.Requests;
using Aizen.Modules.FileStorage.Abstraction.RemoteCall.File.Responses;
using Aizen.Modules.FileStorage.Abstraction.Request.Access;
using Aizen.Modules.FileStorage.Abstraction.Request.File;

namespace Aizen.Modules.Content.Application.UnitTests;

/// <summary>In-memory content item repository. All reads exclude soft-deleted (mirrors the global filter).</summary>
public sealed class InMemoryItemRepo : IContentItemRepository
{
    public readonly List<ContentItemDocument> Store = new();

    private IEnumerable<ContentItemDocument> Live => Store.Where(x => !x.IsDeleted);

    public Task AddAsync(ContentItemDocument d, CancellationToken ct = default) { Store.Add(d); return Task.CompletedTask; }
    public Task ReplaceAsync(ContentItemDocument d, CancellationToken ct = default)
    { var i = Store.FindIndex(x => x.Id == d.Id); if (i >= 0) Store[i] = d; return Task.CompletedTask; }
    // Mirror Mongo semantics: a read returns a fresh snapshot, so a later atomic $inc on the stored
    // document does NOT retroactively change a value the caller already read.
    public Task<ContentItemDocument?> GetByIdAsync(string id, CancellationToken ct = default)
        => Task.FromResult(Live.FirstOrDefault(x => x.Id == id) is { } d ? Clone(d) : null);

    private static ContentItemDocument Clone(ContentItemDocument d) => new()
    {
        Id = d.Id, Type = d.Type, Slug = d.Slug, Status = d.Status, PublishAt = d.PublishAt, ExpireAt = d.ExpireAt,
        DateKey = d.DateKey, DefaultLanguage = d.DefaultLanguage, Translations = d.Translations, Media = d.Media,
        Placements = d.Placements, Audience = d.Audience, Tags = d.Tags, CategorySlug = d.CategorySlug,
        Campaign = d.Campaign, ReleaseNote = d.ReleaseNote, CommentCount = d.CommentCount, FavoriteCount = d.FavoriteCount,
        AuthorUserId = d.AuthorUserId, CreatedAt = d.CreatedAt, UpdatedAt = d.UpdatedAt, PublishedAt = d.PublishedAt,
        IsDeleted = d.IsDeleted,
    };
    public Task<ContentItemDocument?> GetBySlugAsync(string slug, CancellationToken ct = default)
        => Task.FromResult(Live.FirstOrDefault(x => x.Slug == slug));
    public Task<IReadOnlyList<ContentItemDocument>> GetByIdsAsync(IEnumerable<string> ids, CancellationToken ct = default)
    { var set = ids.ToHashSet(); return Task.FromResult((IReadOnlyList<ContentItemDocument>)Live.Where(x => set.Contains(x.Id)).ToList()); }
    public Task<bool> SlugExistsAsync(string slug, string? excludeId = null, CancellationToken ct = default)
        => Task.FromResult(Live.Any(x => x.Slug == slug && x.Id != excludeId));
    public Task<IReadOnlyList<ContentItemDocument>> FindManyAsync(
        Expression<Func<ContentItemDocument, bool>> predicate, int skip = 0, int take = 20, CancellationToken ct = default)
    { var f = predicate.Compile(); return Task.FromResult((IReadOnlyList<ContentItemDocument>)Live.Where(f)
        .OrderByDescending(x => x.CreatedAt).Skip(skip).Take(take).ToList()); }
    public Task<long> CountAsync(Expression<Func<ContentItemDocument, bool>> predicate, CancellationToken ct = default)
    { var f = predicate.Compile(); return Task.FromResult((long)Live.Count(f)); }
    public Task SoftDeleteAsync(string id, CancellationToken ct = default)
    { var d = Live.FirstOrDefault(x => x.Id == id); if (d != null) d.IsDeleted = true; return Task.CompletedTask; }
    public Task IncrementFavoriteCountAsync(string id, int delta, CancellationToken ct = default)
    { var d = Store.FirstOrDefault(x => x.Id == id); if (d != null) d.FavoriteCount += delta; return Task.CompletedTask; }
    public Task IncrementCommentCountAsync(string id, int delta, CancellationToken ct = default)
    { var d = Store.FirstOrDefault(x => x.Id == id); if (d != null) d.CommentCount += delta; return Task.CompletedTask; }
}

public sealed class InMemoryCommentRepo : IContentCommentRepository
{
    public readonly List<ContentCommentDocument> Store = new();
    private IEnumerable<ContentCommentDocument> Live => Store.Where(x => !x.IsDeleted);

    public Task AddAsync(ContentCommentDocument d, CancellationToken ct = default) { Store.Add(d); return Task.CompletedTask; }
    public Task ReplaceAsync(ContentCommentDocument d, CancellationToken ct = default)
    { var i = Store.FindIndex(x => x.Id == d.Id); if (i >= 0) Store[i] = d; return Task.CompletedTask; }
    public Task<ContentCommentDocument?> GetByIdAsync(string id, CancellationToken ct = default)
        => Task.FromResult(Live.FirstOrDefault(x => x.Id == id));
    public Task<IReadOnlyList<ContentCommentDocument>> GetByContentAsync(
        string contentId, ContentCommentStatus? status = null, int skip = 0, int take = 20, CancellationToken ct = default)
        => Task.FromResult((IReadOnlyList<ContentCommentDocument>)Live
            .Where(x => x.ContentId == contentId && (status == null || x.Status == status))
            .OrderByDescending(x => x.CreatedAt).Skip(skip).Take(take).ToList());
    public Task<long> CountByContentAsync(string contentId, ContentCommentStatus? status = null, CancellationToken ct = default)
        => Task.FromResult((long)Live.Count(x => x.ContentId == contentId && (status == null || x.Status == status)));
    public Task<ContentCommentDocument?> GetLatestByAuthorAsync(string contentId, long authorUserId, CancellationToken ct = default)
        => Task.FromResult(Live.Where(x => x.ContentId == contentId && x.AuthorUserId == authorUserId)
            .OrderByDescending(x => x.CreatedAt).FirstOrDefault());
    public Task<IReadOnlyList<ContentCommentDocument>> GetByContentAndAuthorAsync(string contentId, long authorUserId, CancellationToken ct = default)
        => Task.FromResult((IReadOnlyList<ContentCommentDocument>)Live
            .Where(x => x.ContentId == contentId && x.AuthorUserId == authorUserId)
            .OrderByDescending(x => x.CreatedAt).ToList());
    public Task SoftDeleteAsync(string id, CancellationToken ct = default)
    { var d = Live.FirstOrDefault(x => x.Id == id); if (d != null) d.IsDeleted = true; return Task.CompletedTask; }
}

public sealed class InMemoryFavoriteRepo : IContentFavoriteRepository
{
    public readonly List<ContentFavoriteDocument> Store = new();
    private IEnumerable<ContentFavoriteDocument> Live => Store.Where(x => !x.IsDeleted);

    public Task AddAsync(ContentFavoriteDocument d, CancellationToken ct = default) { Store.Add(d); return Task.CompletedTask; }
    public async Task<bool> TryAddAsync(ContentFavoriteDocument d, CancellationToken ct = default)
    { if (await ExistsAsync(d.ContentId, d.UserId, ct)) return false; Store.Add(d); return true; }
    public Task<bool> ExistsAsync(string contentId, long userId, CancellationToken ct = default)
        => Task.FromResult(Live.Any(x => x.ContentId == contentId && x.UserId == userId));
    public Task<bool> RemoveAsync(string contentId, long userId, CancellationToken ct = default)
    { var d = Live.FirstOrDefault(x => x.ContentId == contentId && x.UserId == userId);
      if (d == null) return Task.FromResult(false); d.IsDeleted = true; return Task.FromResult(true); }
    public Task<IReadOnlyList<ContentFavoriteDocument>> GetByUserAsync(long userId, int skip = 0, int take = 20, CancellationToken ct = default)
        => Task.FromResult((IReadOnlyList<ContentFavoriteDocument>)Live.Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt).Skip(skip).Take(take).ToList());
    public Task<long> CountByUserAsync(long userId, CancellationToken ct = default)
        => Task.FromResult((long)Live.Count(x => x.UserId == userId));
}

public sealed class RecordingPublisher : IAizenMessagePublisher
{
    public readonly List<AizenBaseMessage> Published = new();
    public Task PublishAsync<T>(T m, CancellationToken ct = default) where T : AizenBaseMessage { Published.Add(m); return Task.CompletedTask; }
    public Task PublishRollbackAsync<T>(T m, CancellationToken ct = default) where T : AizenBaseMessage => Task.CompletedTask;
    public Task<TR> SendAsync<T, TR>(T m, CancellationToken ct = default) where T : AizenBaseMessage where TR : class => Task.FromResult<TR>(null!);
}

public sealed class RecordingCacheInvalidator : IContentCacheInvalidator
{
    public int Bumps { get; private set; }
    public List<string> LastSurfaces { get; } = new();
    public Task BumpAsync(IEnumerable<ContentSurface> surfaces, CancellationToken ct = default)
    { Bumps++; LastSurfaces.Clear(); LastSurfaces.AddRange(surfaces.Select(s => s.ToString())); return Task.CompletedTask; }
}

public sealed class DictCache : IAizenDistributedCache
{
    private readonly Dictionary<string, object?> _s = new();
    public int Sets { get; private set; }
    public Task<(bool keyExists, T cacheItem)> TryGetAsync<T>(string key, CancellationToken token = default)
        => Task.FromResult(_s.TryGetValue(key, out var v) && v is T t ? (true, t) : (false, default(T)!));
    public Task SetAsync<T>(T item, string key, AizenCacheOptions? o = null, CancellationToken token = default)
    { _s[key] = item; Sets++; return Task.CompletedTask; }

    public Task<T> GetAsync<T>(CancellationToken token = default) => throw new NotImplementedException();
    public Task<T> GetAsync<T>(string key, CancellationToken token = default) => throw new NotImplementedException();
    public Task<(bool keyExists, T cacheItem)> TryGetAsync<T>(CancellationToken token = default) => throw new NotImplementedException();
    public Task SetAsync<T>(T item, AizenCacheOptions? o = null, CancellationToken token = default) => throw new NotImplementedException();
    public Task<bool> ExistsAsync<T>(CancellationToken token = default) => throw new NotImplementedException();
    public Task<bool> ExistsAsync<T>(string key, CancellationToken token = default) => throw new NotImplementedException();
    public Task RemoveAsync<T>(CancellationToken token = default) => throw new NotImplementedException();
    public Task RemoveAsync<T>(string key, CancellationToken token = default) => throw new NotImplementedException();
    public Task<T> GetNoHash<T>(string key, CancellationToken token = default) => throw new NotImplementedException();
    public Task<dynamic> GetDirectStringToDynamic(string key, CancellationToken token = default) => throw new NotImplementedException();
    public Task<bool> RemoveNoHash(string key, CancellationToken token = default) => throw new NotImplementedException();
    public Task<bool> RemoveReadCacheEntry(string key, CancellationToken token = default) => throw new NotImplementedException();
    public Task<bool> ExistNoHash(string key, CancellationToken token = default) => throw new NotImplementedException();
    public Task<bool> SetNoHash<T>(string key, T value, TimeSpan ttl, CancellationToken token = default) => throw new NotImplementedException();
    public Task<AizenStringCacheItem<T>> GetNoHashWitTtl<T>(string key, CancellationToken token = default) => throw new NotImplementedException();
}

public sealed class FakeInfo : IAizenInfoAccessor
{
    public long UserId { get; init; }
    public string[] Roles { get; init; } = System.Array.Empty<string>();
    public string AccessToken { get; init; } = "test-token";
    public IAizenUserInfoAccessor UserInfoAccessor => new FU(UserId, Roles, AccessToken);
    public IAizenChannelInfoAccessor ChannelInfoAccessor => null!;
    public IAizenClientInfoAccessor ClientInfoAccessor => null!;
    public IAizenDeviceInfoAccessor DeviceInfoAccessor => null!;
    public IAizenExecutionInfoAccessor ExecutionInfoAccessor => null!;
    public IAizenAppInfoAccessor AppInfoAccessor => null!;
    public IAizenServerInfoAccessor ServerInfoAccessor => null!;
    public IAizenNetworkInfoAccessor NetworkInfoAccessor => null!;
    public IAizenRequestInfoAccessor RequestInfoAccessor => null!;
    public IAizenKeycloakTokenInfoAccessor KeycloakTokenInfoAccessor => null!;
    public TInfo GetInfo<TInfo>() where TInfo : IAizenInfo => default!;

    private sealed class FU(long uid, string[] roles, string tok) : IAizenUserInfoAccessor
    {
        public AizenUserInfo UserInfo { get; } = new() { UserId = uid, Roles = roles, AccessToken = tok };
    }
}

/// <summary>Every file id exists except <see cref="MissingId"/>.</summary>
public sealed class FakeFileStorage : IFileStorageRemoteCall
{
    public static readonly Guid MissingId = Guid.Parse("dead0000-0000-0000-0000-000000000000");
    public Task<AizenApiResponse<FileMetadataDto>> GetFileMetadata(Guid fileId, string authorization)
        => Task.FromResult(fileId == MissingId
            ? new AizenApiResponse<FileMetadataDto>()
            : new AizenApiResponse<FileMetadataDto>(AizenResponseHeader.Success(), new FileMetadataDto { FileId = fileId }));
    public Task<AizenApiResponse<FileAccessUrlDto>> CreateReadUrl(Guid f, CreateReadUrlRequest r, string a) => throw new NotImplementedException();
    public Task<AizenApiResponse<FileOwnerReferenceDto>> LinkFileToOwner(Guid f, LinkFileToOwnerRequest r, string a) => throw new NotImplementedException();
    public Task<ValidateFileOwnershipRemoteCallResponse> ValidateFileOwnership(Guid f, ValidateFileOwnershipRemoteCallRequest r, string a) => throw new NotImplementedException();
    public Task<CreateUploadSessionRemoteCallResponse> CreateUploadSession(CreateUploadSessionRemoteCallRequest r, string a) => throw new NotImplementedException();
    public Task<CompleteUploadSessionRemoteCallResponse> CompleteUploadSession(string c, CompleteUploadSessionRemoteCallRequest r, string a) => throw new NotImplementedException();
    public Task<DeleteFileRemoteCallResponse> DeleteFile(Guid f, DeleteFileRemoteCallRequest r, string a) => throw new NotImplementedException();
}
