using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Aizen.Bff.MarineProvider.Application.Common.Services.PasswordRecovery;

/// <summary>
/// Persistence for in-flight password recovery requests. Backed by the platform distributed cache (Redis)
/// so the flow survives across BFF instances. Keyed by the opaque resetRequestId.
/// </summary>
public interface IProviderPasswordRecoveryStore
{
    Task SaveAsync(PasswordRecoveryRecord record, TimeSpan ttl, CancellationToken cancellationToken = default);
    Task<PasswordRecoveryRecord?> GetAsync(string resetRequestId, CancellationToken cancellationToken = default);
    Task RemoveAsync(string resetRequestId, CancellationToken cancellationToken = default);
}

internal sealed class DistributedProviderPasswordRecoveryStore : IProviderPasswordRecoveryStore
{
    private const string KeyPrefix = "provider:pwreset:";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IDistributedCache _cache;

    public DistributedProviderPasswordRecoveryStore(IDistributedCache cache) => _cache = cache;

    private static string Key(string resetRequestId) => KeyPrefix + resetRequestId;

    public async Task SaveAsync(PasswordRecoveryRecord record, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(record, Json);
        await _cache.SetAsync(
            Key(record.ResetRequestId),
            payload,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
            cancellationToken);
    }

    public async Task<PasswordRecoveryRecord?> GetAsync(string resetRequestId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(resetRequestId)) return null;
        var payload = await _cache.GetAsync(Key(resetRequestId), cancellationToken);
        return payload is null ? null : JsonSerializer.Deserialize<PasswordRecoveryRecord>(payload, Json);
    }

    public Task RemoveAsync(string resetRequestId, CancellationToken cancellationToken = default)
        => _cache.RemoveAsync(Key(resetRequestId), cancellationToken);
}
