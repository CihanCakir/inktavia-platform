using Aizen.Core.Cache.Abstraction;
using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Modules.Identity.Domain.Interface.Service;

namespace Aizen.Modules.Identity.Repository.UnitTests.EmailVerification;

/// <summary>
/// Süreç-içi sahte dağıtık cache. Yalnızca <see cref="GetNoHash{T}"/> / <see cref="SetNoHash{T}"/> desteklenir
/// (yeniden gönderme hız sınırı + soğuma sayaçları için); testler senkron olduğundan TTL yok sayılır.
/// </summary>
internal sealed class FakeDistributedCache : IAizenDistributedCache
{
    private readonly Dictionary<string, object?> _store = new();

    public Task<T> GetNoHash<T>(string key, CancellationToken token = default)
    {
        if (_store.TryGetValue(key, out var v) && v is T typed)
            return Task.FromResult(typed);
        return Task.FromResult(default(T)!);
    }

    public Task<bool> SetNoHash<T>(string key, T value, TimeSpan ttl, CancellationToken token = default)
    {
        _store[key] = value;
        return Task.FromResult(true);
    }

    // ── Kullanılmayan üyeler ────────────────────────────────────────────────
    public Task<dynamic> GetDirectStringToDynamic(string key, CancellationToken token = default) => throw new NotSupportedException();
    public Task<bool> RemoveNoHash(string key, CancellationToken token = default) => throw new NotSupportedException();
    public Task<bool> RemoveReadCacheEntry(string key, CancellationToken token = default) => throw new NotSupportedException();
    public Task<bool> ExistNoHash(string key, CancellationToken token = default) => throw new NotSupportedException();
    public Task<AizenStringCacheItem<T>> GetNoHashWitTtl<T>(string key, CancellationToken token = default) => throw new NotSupportedException();
    public Task<T> GetAsync<T>(CancellationToken token = default) => throw new NotSupportedException();
    public Task<T> GetAsync<T>(string key, CancellationToken token = default) => throw new NotSupportedException();
    public Task<(bool keyExists, T cacheItem)> TryGetAsync<T>(CancellationToken token = default) => throw new NotSupportedException();
    public Task<(bool keyExists, T cacheItem)> TryGetAsync<T>(string key, CancellationToken token = default) => throw new NotSupportedException();
    public Task SetAsync<T>(T cacheItem, AizenCacheOptions? cacheOptions = null, CancellationToken token = default) => throw new NotSupportedException();
    public Task SetAsync<T>(T cacheItem, string key, AizenCacheOptions? cacheOptions = null, CancellationToken token = default) => throw new NotSupportedException();
    public Task<bool> ExistsAsync<T>(CancellationToken token = default) => throw new NotSupportedException();
    public Task<bool> ExistsAsync<T>(string key, CancellationToken token = default) => throw new NotSupportedException();
    public Task RemoveAsync<T>(CancellationToken token = default) => throw new NotSupportedException();
    public Task RemoveAsync<T>(string key, CancellationToken token = default) => throw new NotSupportedException();
}

/// <summary>Dağıtım sayısını ve son doğrulama linkini kaydeden sahte notifier.</summary>
internal sealed class FakeEmailVerificationNotifier : IProviderEmailVerificationNotifier
{
    public int DispatchCount { get; private set; }
    public string? LastVerifyUrl { get; private set; }

    public Task SendVerificationAsync(
        long recipientUserId, string email, string maskedTarget, string verifyUrl,
        int expiresInMinutes, CancellationToken cancellationToken = default)
    {
        DispatchCount++;
        LastVerifyUrl = verifyUrl;
        return Task.CompletedTask;
    }
}
