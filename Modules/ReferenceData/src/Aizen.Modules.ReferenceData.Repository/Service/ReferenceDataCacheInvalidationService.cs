using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Repository.Service;

public sealed class ReferenceDataCacheInvalidationService : IReferenceDataCacheInvalidationService
{
    private readonly IReferenceDataCacheKeyService _keys;

    public ReferenceDataCacheInvalidationService(IReferenceDataCacheKeyService keys)
    {
        _keys = keys;
    }

    public Task InvalidateCurrencyAsync(long? id = null, CancellationToken cancellationToken = default)
    {
        // Cache invalidation hooks can be added here when a cache provider is available.
        return Task.CompletedTask;
    }

    public Task InvalidateExchangeRateAsync(string? fromCode = null, string? toCode = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task InvalidateLookupGroupAsync(long? id = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task InvalidateLookupItemAsync(string? groupCode = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task InvalidateMeasurementUnitAsync(long? id = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task InvalidateLocationAsync(string? countryCode = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task InvalidateSystemParameterAsync(string? key = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
