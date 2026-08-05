using Aizen.Core.Cache.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.RemoteCall;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.ServiceRequest.Application.Services.Pricing;

/// <summary>
/// S2c consumer — resolves R4 marine lookup items for a group via the ReferenceData remote call, cached in Redis (long
/// TTL; lookups change ~never). Unlike unit validation, pricing-attribute lookup resolution is <b>fail-loud</b>: a remote
/// failure throws rather than silently skipping, so an offer can never capture an unvalidated lookup value. An unknown
/// group resolves to an <b>empty</b> list (the ReferenceData query returns empty, never throws) — callers treat empty as
/// "unknown group" for a Lookup definition/value.
/// </summary>
public sealed class ReferenceDataLookupClient
{
    private const string CacheKeyPrefix = "refdata:lookup-items:";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);

    private readonly IServiceRequestReferenceDataRemoteCall _referenceData;
    private readonly IAizenDistributedCache _cache;
    private readonly ILogger<ReferenceDataLookupClient> _logger;

    public ReferenceDataLookupClient(
        IServiceRequestReferenceDataRemoteCall referenceData,
        IAizenDistributedCache cache,
        ILogger<ReferenceDataLookupClient> logger)
    {
        _referenceData = referenceData;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>Active items of a lookup group (cached). Empty ⇒ unknown group or no active items.</summary>
    public async Task<IReadOnlyList<SrLookupItemDto>> GetActiveItemsAsync(string groupCode, CancellationToken ct)
    {
        var normalized = (groupCode ?? string.Empty).Trim().ToUpperInvariant();
        if (normalized.Length == 0) return Array.Empty<SrLookupItemDto>();

        var cacheKey = CacheKeyPrefix + normalized;
        try
        {
            var cached = await _cache.GetNoHash<List<SrLookupItemDto>>(cacheKey);
            if (cached is not null) return cached;
        }
        catch { /* cache miss/error → fall through to remote */ }

        List<SrLookupItemDto> items;
        try
        {
            var response = await _referenceData.GetLookupItemsByGroup(normalized, true);
            items = response?.Body ?? new List<SrLookupItemDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ReferenceData lookup group {Group} could not be resolved.", normalized);
            throw new AizenBusinessException($"SR_PRICING_ATTR_LOOKUP_UNAVAILABLE: '{normalized}'");
        }

        // Cache only positive results — never cache an empty/unknown group (a later seed must be visible before TTL).
        if (items.Count > 0)
        {
            try { await _cache.SetNoHash(cacheKey, items, CacheTtl); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to cache lookup items for {Group}.", normalized); }
        }
        return items;
    }

    /// <summary>True when the group resolves to at least one active R4 item (used for S2a group-resolution, fail-loud).</summary>
    public async Task<bool> GroupResolvesAsync(string groupCode, CancellationToken ct)
        => (await GetActiveItemsAsync(groupCode, ct)).Count > 0;

    /// <summary>Resolve an item's Turkish display label (denormalized into the S2d snapshot). Null when not a member.</summary>
    public async Task<string?> ResolveItemLabelAsync(string groupCode, string itemCode, CancellationToken ct)
    {
        var code = (itemCode ?? string.Empty).Trim().ToUpperInvariant();
        var item = (await GetActiveItemsAsync(groupCode, ct)).FirstOrDefault(i => i.Code == code);
        return item?.Name;
    }
}
