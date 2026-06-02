using System.Security.Cryptography;
using System.Text;
using Aizen.Core.Cache.Abstraction;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Repository.Service;

/// <summary>
/// Invalidates caches stored by the AizenQueryHandlerDecorator.
/// The decorator stores entries under keys: "{HandlerTypeName}:{SHA256hex(propString)}"
/// where propString = concatenation of "PropName_Value|" for each non-QueryId property.
/// This service replicates that key computation to perform exact-match invalidation.
/// </summary>
public sealed class ReferenceDataCacheInvalidationService : IReferenceDataCacheInvalidationService
{
    private readonly IAizenDistributedCache _cache;

    public ReferenceDataCacheInvalidationService(IAizenDistributedCache cache)
    {
        _cache = cache;
    }

    // ─── Key helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Computes the exact cache key format used by AizenQueryHandlerDecorator:
    /// "{handlerTypeName}:{SHA256lowerhex(propString)}"
    /// </summary>
    private static string HandlerKey(string handlerTypeName, string propString)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(propString));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes) sb.AppendFormat("{0:x2}", b);
        return $"{handlerTypeName}:{sb}";
    }

    /// <summary>
    /// Key for zero-property query handlers (no SHA256 hash, empty suffix).
    /// </summary>
    private static string HandlerKeyNoProps(string handlerTypeName) => $"{handlerTypeName}:";

    // ─── Currency ───────────────────────────────────────────────────────────

    public async Task InvalidateCurrencyAsync(long? id = null, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveNoHash(HandlerKey("GetCurrencyListQueryHandler", "OnlyActive_True|"));
        await _cache.RemoveNoHash(HandlerKey("GetCurrencyListQueryHandler", "OnlyActive_False|"));
        await _cache.RemoveNoHash(HandlerKeyNoProps("GetBaseCurrencyQueryHandler"));
        if (id.HasValue)
            await _cache.RemoveNoHash(HandlerKey("GetCurrencyDetailQueryHandler", $"Id_{id.Value}|"));
    }

    // ─── ExchangeRate ────────────────────────────────────────────────────────

    public async Task InvalidateExchangeRateAsync(string? fromCode = null, string? toCode = null, CancellationToken cancellationToken = default)
    {
        if (fromCode != null && toCode != null)
        {
            await _cache.RemoveNoHash(HandlerKey("GetExchangeRateQueryHandler", $"FromCurrencyCode_{fromCode}|ToCurrencyCode_{toCode}|"));
            await _cache.RemoveNoHash(HandlerKey("GetExchangeRatesByCurrencyQueryHandler", $"CurrencyCode_{fromCode}|"));
            await _cache.RemoveNoHash(HandlerKey("GetExchangeRatesByCurrencyQueryHandler", $"CurrencyCode_{toCode}|"));
        }
        // TODO: Wildcard removal is not supported — full exchange-rate cache flush requires knowing all pairs
    }

    // ─── Lookup ──────────────────────────────────────────────────────────────

    public async Task InvalidateLookupGroupAsync(long? id = null, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveNoHash(HandlerKey("GetLookupGroupListQueryHandler", "OnlyActive_True|"));
        await _cache.RemoveNoHash(HandlerKey("GetLookupGroupListQueryHandler", "OnlyActive_False|"));
        await _cache.RemoveNoHash(HandlerKey("GetLookupGroupTreeQueryHandler", "OnlyActive_True|"));
        await _cache.RemoveNoHash(HandlerKey("GetLookupGroupTreeQueryHandler", "OnlyActive_False|"));
        if (id.HasValue)
            await _cache.RemoveNoHash(HandlerKey("GetLookupGroupDetailQueryHandler", $"Id_{id.Value}|"));
    }

    public async Task InvalidateLookupItemAsync(string? groupCode = null, CancellationToken cancellationToken = default)
    {
        // Lookup tree is also affected by item changes
        await _cache.RemoveNoHash(HandlerKey("GetLookupGroupTreeQueryHandler", "OnlyActive_True|"));
        await _cache.RemoveNoHash(HandlerKey("GetLookupGroupTreeQueryHandler", "OnlyActive_False|"));
        if (groupCode != null)
        {
            await _cache.RemoveNoHash(HandlerKey("GetLookupItemsByGroupQueryHandler", $"GroupCode_{groupCode}|OnlyActive_True|"));
            await _cache.RemoveNoHash(HandlerKey("GetLookupItemsByGroupQueryHandler", $"GroupCode_{groupCode}|OnlyActive_False|"));
        }
        // TODO: When groupCode is null, cannot invalidate all group-item caches without wildcard support
    }

    // ─── Measurement ─────────────────────────────────────────────────────────

    public async Task InvalidateMeasurementUnitAsync(long? id = null, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveNoHash(HandlerKey("GetMeasurementUnitListQueryHandler", "OnlyActive_True|"));
        await _cache.RemoveNoHash(HandlerKey("GetMeasurementUnitListQueryHandler", "OnlyActive_False|"));
        if (id.HasValue)
            await _cache.RemoveNoHash(HandlerKey("GetMeasurementUnitDetailQueryHandler", $"Id_{id.Value}|"));
        // TODO: GetMeasurementUnitsByTypeQueryHandler — unit type is not available in the invalidation context
    }

    // ─── Location ────────────────────────────────────────────────────────────

    public async Task InvalidateLocationAsync(string? countryCode = null, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveNoHash(HandlerKey("GetCountriesQueryHandler", "OnlyActive_True|"));
        await _cache.RemoveNoHash(HandlerKey("GetCountriesQueryHandler", "OnlyActive_False|"));
        if (countryCode != null)
        {
            await _cache.RemoveNoHash(HandlerKey("GetCountryDetailQueryHandler", $"CountryCode_{countryCode}|"));
            await _cache.RemoveNoHash(HandlerKey("GetCitiesByCountryQueryHandler", $"CountryCode_{countryCode}|OnlyActive_True|"));
            await _cache.RemoveNoHash(HandlerKey("GetCitiesByCountryQueryHandler", $"CountryCode_{countryCode}|OnlyActive_False|"));
        }
    }

    public async Task InvalidateDistrictAsync(string countryCode, string cityCode, CancellationToken cancellationToken = default)
    {
        // Invalidate district list for this city
        await _cache.RemoveNoHash(HandlerKey("GetDistrictsByCityQueryHandler", $"CountryCode_{countryCode}|CityCode_{cityCode}|OnlyActive_True|"));
        await _cache.RemoveNoHash(HandlerKey("GetDistrictsByCityQueryHandler", $"CountryCode_{countryCode}|CityCode_{cityCode}|OnlyActive_False|"));
        // Also invalidate city detail cache since district changes may affect it
        await _cache.RemoveNoHash(HandlerKey("GetCityDetailQueryHandler", $"CountryCode_{countryCode}|CityCode_{cityCode}|"));
    }

    public async Task InvalidateNeighborhoodAsync(string countryCode, string cityCode, string districtCode, CancellationToken cancellationToken = default)
    {
        // Invalidate neighborhood list for this district
        await _cache.RemoveNoHash(HandlerKey("GetNeighborhoodsByDistrictQueryHandler", $"CountryCode_{countryCode}|CityCode_{cityCode}|DistrictCode_{districtCode}|OnlyActive_True|"));
        await _cache.RemoveNoHash(HandlerKey("GetNeighborhoodsByDistrictQueryHandler", $"CountryCode_{countryCode}|CityCode_{cityCode}|DistrictCode_{districtCode}|OnlyActive_False|"));
        // Also cascade up to district cache
        await InvalidateDistrictAsync(countryCode, cityCode, cancellationToken);
    }

    // ─── SystemParameter ─────────────────────────────────────────────────────

    public async Task InvalidateSystemParameterAsync(string? key = null, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveNoHash(HandlerKey("GetSystemParameterListQueryHandler", "OnlyActive_True|"));
        await _cache.RemoveNoHash(HandlerKey("GetSystemParameterListQueryHandler", "OnlyActive_False|"));
        if (key != null)
            await _cache.RemoveNoHash(HandlerKey("GetSystemParameterByKeyQueryHandler", $"Key_{key}|"));
        // TODO: Prefix-based invalidation (GetSystemParametersByPrefixQueryHandler) requires knowing the prefix
    }
}
