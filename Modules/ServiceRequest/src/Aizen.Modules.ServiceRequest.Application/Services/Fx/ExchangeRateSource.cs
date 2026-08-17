using Aizen.Core.Cache.Abstraction;
using Aizen.Modules.ServiceRequest.Abstraction.RemoteCall;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.ServiceRequest.Application.Services.Fx;

/// <summary>
/// BE-S3 — the offer-FX resolver's <b>rate-fetch seam</b>: a single point-in-time R1 lookup for one currency pair. Kept as
/// a narrow interface so <see cref="OfferFxResolver"/> (which owns the fail-loud + conversion policy) is unit-testable with
/// a trivial fake, while the real implementation adds the Redis cache + remote call.
/// </summary>
public interface IExchangeRateSource
{
    /// <summary>Resolves the effective rate at the instant. Returns the R1 DTO (with <c>HasRate=false</c> when none is effective); never throws for a missing rate.</summary>
    Task<SrExchangeRateResolveDto?> GetAsync(string fromCurrencyCode, string toCurrencyCode, DateTimeOffset asOfUtc, CancellationToken ct);
}

/// <summary>
/// BE-S3 — R1 point-in-time FX resolve via the ReferenceData remote call, cached in Redis by <c>from:to:as-of-day</c>
/// (rates are daily; per convention R1 is cached by from/to/as-of-day). Positive results only are cached; a not-found
/// (<c>HasRate=false</c>) is never cached so a later rate insert becomes visible before TTL. Cache errors fail open (fall
/// through to remote). The <b>fail-loud</b> decision (missing rate → reject the submit) lives in <see cref="OfferFxResolver"/>,
/// not here — this layer only fetches.
/// </summary>
public sealed class ExchangeRateSource : IExchangeRateSource
{
    private const string CacheKeyPrefix = "sr:fx-resolve:";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(6);

    private readonly IServiceRequestReferenceDataRemoteCall _referenceData;
    private readonly IAizenDistributedCache _cache;
    private readonly ILogger<ExchangeRateSource> _logger;

    public ExchangeRateSource(
        IServiceRequestReferenceDataRemoteCall referenceData,
        IAizenDistributedCache cache,
        ILogger<ExchangeRateSource> logger)
    {
        _referenceData = referenceData;
        _cache = cache;
        _logger = logger;
    }

    public async Task<SrExchangeRateResolveDto?> GetAsync(
        string fromCurrencyCode, string toCurrencyCode, DateTimeOffset asOfUtc, CancellationToken ct)
    {
        var from = (fromCurrencyCode ?? string.Empty).Trim().ToUpperInvariant();
        var to   = (toCurrencyCode ?? string.Empty).Trim().ToUpperInvariant();
        var day  = asOfUtc.UtcDateTime.ToString("yyyyMMdd");
        var cacheKey = $"{CacheKeyPrefix}{from}:{to}:{day}";

        try
        {
            var cached = await _cache.GetNoHash<SrExchangeRateResolveDto>(cacheKey);
            if (cached is not null) return cached;
        }
        catch { /* cache miss/error → fall through to remote */ }

        var response = await _referenceData.ResolveExchangeRate(from, to, asOfUtc);
        var dto = response?.Body;

        // Cache only a positive, sane result — never a "no rate" (a later rate insert must be visible before TTL).
        if (dto is { HasRate: true } && dto.Rate > 0m)
        {
            try { await _cache.SetNoHash(cacheKey, dto, CacheTtl); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to cache FX rate {From}->{To} @ {Day}.", from, to, day); }
        }

        return dto;
    }
}
