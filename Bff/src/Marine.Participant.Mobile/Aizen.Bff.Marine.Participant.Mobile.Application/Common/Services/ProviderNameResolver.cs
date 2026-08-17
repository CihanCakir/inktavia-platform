using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;

/// <summary>
/// Resolves provider profile ids → display names, BFF-side. The ServiceRequest module returns only the
/// providerProfileId; surfaces that want to show a provider's name (owner offers, and later disputes / jobs /
/// conversations) call this instead of re-implementing the lookup. Shared, cost-free: it hands back names only —
/// the caller never exposes the profile id downstream.
/// </summary>
public interface IProviderNameResolver
{
    /// <summary>
    /// One batch Identity call for the whole set (no N+1) → an id→displayName map. A missing/unknown id (or a
    /// failed lookup) maps to <c>null</c> so the caller keeps its own fallback. Results are briefly cached so a
    /// provider that appears across several offers/screens is fetched once.
    /// </summary>
    Task<IReadOnlyDictionary<long, string?>> ResolveAsync(
        IEnumerable<long> providerProfileIds, CancellationToken ct = default);
}

public sealed class ProviderNameResolver : IProviderNameResolver
{
    private readonly IIdentityRemoteCall _identity;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ProviderNameResolver> _logger;

    // Short TTL: names change rarely; a brief cache collapses repeat lookups across a session without going stale.
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan NegativeTtl = TimeSpan.FromMinutes(2);
    private const string KeyPrefix = "provider:name:";

    public ProviderNameResolver(
        IIdentityRemoteCall identity, IMemoryCache cache, ILogger<ProviderNameResolver> logger)
    {
        _identity = identity;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IReadOnlyDictionary<long, string?>> ResolveAsync(
        IEnumerable<long> providerProfileIds, CancellationToken ct = default)
    {
        var result = new Dictionary<long, string?>();
        var distinct = providerProfileIds.Where(id => id > 0).Distinct().ToArray();
        if (distinct.Length == 0)
            return result;

        // Serve what the cache already knows; batch only the rest.
        var uncached = new List<long>();
        foreach (var id in distinct)
        {
            if (_cache.TryGetValue(KeyPrefix + id, out string? cachedName))
                result[id] = cachedName;
            else
                uncached.Add(id);
        }

        if (uncached.Count == 0)
            return result;

        try
        {
            // ONE call for every uncached id — no N+1. The endpoint computes the display name (CompanyName-first)
            // server-side and is IdentityRead-callable by the mobile-bff service token (no admin needed).
            var resp = await _identity.GetProfileDisplayNamesByProfileIds(uncached.ToArray());
            var items = resp?.Body ?? new();

            foreach (var p in items)
            {
                var name = string.IsNullOrWhiteSpace(p.DisplayName) ? null : p.DisplayName!.Trim();
                result[p.ProfileId] = name;
                _cache.Set(KeyPrefix + p.ProfileId, name, Ttl);
            }

            // Ids the lookup didn't return (unknown / not visible) → null, briefly negative-cached.
            foreach (var id in uncached)
            {
                if (result.ContainsKey(id)) continue;
                result[id] = null;
                _cache.Set(KeyPrefix + id, (string?)null, NegativeTtl);
            }
        }
        catch (Exception ex)
        {
            // Never fail the caller for a display nicety — the FE keeps its localized fallback.
            _logger.LogWarning(ex, "Provider-name resolution failed for {Count} profile id(s); names fall back.", uncached.Count);
            foreach (var id in uncached)
                result.TryAdd(id, null);
        }

        return result;
    }
}
