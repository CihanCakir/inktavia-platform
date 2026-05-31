namespace Aizen.Modules.ReferenceData.Domain.Interface.Service;

public interface IReferenceDataCacheInvalidationService
{
    Task InvalidateCurrencyAsync(long? id = null, CancellationToken cancellationToken = default);
    Task InvalidateExchangeRateAsync(string? fromCode = null, string? toCode = null, CancellationToken cancellationToken = default);
    Task InvalidateLookupGroupAsync(long? id = null, CancellationToken cancellationToken = default);
    Task InvalidateLookupItemAsync(string? groupCode = null, CancellationToken cancellationToken = default);
    Task InvalidateMeasurementUnitAsync(long? id = null, CancellationToken cancellationToken = default);
    Task InvalidateLocationAsync(string? countryCode = null, CancellationToken cancellationToken = default);
    Task InvalidateSystemParameterAsync(string? key = null, CancellationToken cancellationToken = default);
}
