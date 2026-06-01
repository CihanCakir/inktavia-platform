using Aizen.Core.Cache.Abstraction;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Repository.Service;

public sealed class ReferenceDataCacheInvalidationService : IReferenceDataCacheInvalidationService
{
    private readonly IReferenceDataCacheKeyService _keys;
    private readonly IAizenDistributedCache _cache;

    public ReferenceDataCacheInvalidationService(IReferenceDataCacheKeyService keys, IAizenDistributedCache cache)
    {
        _keys = keys;
        _cache = cache;
    }

    public async Task InvalidateCurrencyAsync(long? id = null, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveNoHash(_keys.CurrencyList());
        await _cache.RemoveNoHash(_keys.BaseCurrency());
        if (id.HasValue)
            await _cache.RemoveNoHash(_keys.CurrencyDetail(id.Value));
    }

    public async Task InvalidateExchangeRateAsync(string? fromCode = null, string? toCode = null, CancellationToken cancellationToken = default)
    {
        if (fromCode != null && toCode != null)
        {
            await _cache.RemoveNoHash(_keys.ExchangeRate(fromCode, toCode));
            await _cache.RemoveNoHash(_keys.ExchangeRatesByCurrency(fromCode));
            await _cache.RemoveNoHash(_keys.ExchangeRatesByCurrency(toCode));
        }
    }

    public async Task InvalidateLookupGroupAsync(long? id = null, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveNoHash(_keys.LookupGroupList());
        await _cache.RemoveNoHash(_keys.LookupGroupTree());
        if (id.HasValue)
            await _cache.RemoveNoHash(_keys.LookupGroupDetail(id.Value));
    }

    public async Task InvalidateLookupItemAsync(string? groupCode = null, CancellationToken cancellationToken = default)
    {
        if (groupCode != null)
            await _cache.RemoveNoHash(_keys.LookupItemsByGroup(groupCode));
    }

    public async Task InvalidateMeasurementUnitAsync(long? id = null, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveNoHash(_keys.MeasurementUnitList());
        if (id.HasValue)
            await _cache.RemoveNoHash(_keys.MeasurementUnitDetail(id.Value));
    }

    public async Task InvalidateLocationAsync(string? countryCode = null, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveNoHash(_keys.CountryList());
        if (countryCode != null)
        {
            await _cache.RemoveNoHash(_keys.CountryDetail(countryCode));
            await _cache.RemoveNoHash(_keys.CitiesByCountry(countryCode));
        }
    }

    public async Task InvalidateSystemParameterAsync(string? key = null, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveNoHash(_keys.SystemParameterList());
        if (key != null)
            await _cache.RemoveNoHash(_keys.SystemParameter(key));
    }
}
