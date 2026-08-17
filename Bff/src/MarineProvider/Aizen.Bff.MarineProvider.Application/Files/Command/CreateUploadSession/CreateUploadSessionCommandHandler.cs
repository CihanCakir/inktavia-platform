using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Bff.MarineProvider.Application.Contracts.Files;
using Aizen.Core.Cache.Abstraction;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Abstraction.Request.UploadSession;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.Files;

public sealed class CreateUploadSessionCommandHandler
    : AizenCommandHandler<CreateUploadSessionCommand, CreateUploadSessionBffResponse>
{
    private readonly IFileStorageRemoteCall _fileStorage;
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _identityHolder;
    private readonly IAizenDistributedCache _cache;
    private readonly ILogger<CreateUploadSessionCommandHandler> _logger;

    private const string SessionRateLimitKeyPrefix = "upload:session:";
    private const string BytesRateLimitKeyPrefix = "upload:bytes:";
    private const int MaxSessionsPerHour = 20;
    private const long MaxPendingBytes = 100 * 1024 * 1024; // 100 MB
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromHours(1);

    public CreateUploadSessionCommandHandler(
        IFileStorageRemoteCall fileStorage,
        IProviderProfileResolver resolver,
        IProviderIdentityHolder identityHolder,
        IAizenDistributedCache cache,
        ILogger<CreateUploadSessionCommandHandler> logger)
    {
        _fileStorage = fileStorage;
        _resolver = resolver;
        _identityHolder = identityHolder;
        _cache = cache;
        _logger = logger;
    }

    public override async Task<CreateUploadSessionBffResponse?> Handle(CreateUploadSessionCommand request, CancellationToken ct)
    {
        // Resolve identity first → populates IProviderIdentityHolder → assertion headers on the module call.
        // FileStorage stamps UploadedByUserId from the asserted user; without this the file is owned by user 0
        // and can never pass the ownership check when it is attached to a profile.
        await _resolver.ResolveAsync(ct);
        if (_identityHolder.UserId is not > 0)
        {
            _logger.LogError("Provider identity unresolved; refusing to create an upload session.");
            return null;
        }

        var userId = _identityHolder.UserId!.Value;

        // ── Per-provider rate limiting ──────────────────────────────────────
        if (await IsSessionLimitExceededAsync(userId) || await IsBytesLimitExceededAsync(userId, request.Size))
            return null;

        var category = System.Enum.TryParse<FileCategory>(request.Category, true, out var cat) ? cat : FileCategory.Document;
        var result = await _fileStorage.CreateUploadSession(new CreateUploadSessionRequest
        {
            OriginalFileName = request.FileName,
            ContentType = request.ContentType,
            SizeInBytes = request.Size,
            Category = category,
            Visibility = FileVisibility.Private,
        });

        var data = result.Body;
        if (data is null) return null;

        // Counters incremented after successful session creation
        await IncrementSessionCounterAsync(userId);
        await IncrementBytesCounterAsync(userId, request.Size);

        // Strip BucketName and ObjectKey — never expose to the frontend
        return new CreateUploadSessionBffResponse
        {
            FileId = data.FileId,
            UploadSessionCode = data.UploadSessionCode,
            UploadUrl = data.UploadUrl,
            ExpiresAt = data.ExpiresAt,
            RequiredHeaders = new Dictionary<string, string>
            {
                ["Content-Type"] = request.ContentType,
            },
        };
    }

    private async Task<bool> IsSessionLimitExceededAsync(long userId)
    {
        var key = $"{SessionRateLimitKeyPrefix}{userId}";
        try
        {
            var current = await _cache.GetNoHash<int>(key);
            if (current >= MaxSessionsPerHour)
            {
                _logger.LogWarning("Upload session limit exceeded for user {UserId}.", userId);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Upload session rate-limit cache error; failing open.");
            return false;
        }
    }

    private async Task<bool> IsBytesLimitExceededAsync(long userId, long requestSize)
    {
        var key = $"{BytesRateLimitKeyPrefix}{userId}";
        try
        {
            var currentBytes = await _cache.GetNoHash<long>(key);
            if (currentBytes + requestSize > MaxPendingBytes)
            {
                _logger.LogWarning("Upload bytes limit exceeded for user {UserId}. Current: {CurrentBytes}, Requested: {RequestSize}.",
                    userId, currentBytes, requestSize);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Upload bytes rate-limit cache error; failing open.");
            return false;
        }
    }

    private async Task IncrementSessionCounterAsync(long userId)
    {
        var key = $"{SessionRateLimitKeyPrefix}{userId}";
        try
        {
            var current = await _cache.GetNoHash<int>(key);
            await _cache.SetNoHash(key, current + 1, RateLimitWindow);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to increment upload session counter for user {UserId}.", userId);
        }
    }

    private async Task IncrementBytesCounterAsync(long userId, long sizeInBytes)
    {
        var key = $"{BytesRateLimitKeyPrefix}{userId}";
        try
        {
            var currentBytes = await _cache.GetNoHash<long>(key);
            await _cache.SetNoHash(key, currentBytes + sizeInBytes, RateLimitWindow);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to increment upload bytes counter for user {UserId}.", userId);
        }
    }
}
