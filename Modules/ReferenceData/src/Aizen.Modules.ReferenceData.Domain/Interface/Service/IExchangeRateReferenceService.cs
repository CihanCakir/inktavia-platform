using Aizen.Modules.ReferenceData.Abstraction.Dto.ExchangeRate;
using Aizen.Modules.ReferenceData.Abstraction.Request.ExchangeRate;

namespace Aizen.Modules.ReferenceData.Domain.Interface.Service;

public interface IExchangeRateReferenceService
{
    Task<ExchangeRateDto> UpdateRateAsync(UpdateExchangeRateRequest request, CancellationToken cancellationToken = default);
    Task SyncRatesAsync(IEnumerable<UpdateExchangeRateRequest> requests, CancellationToken cancellationToken = default);
    Task<ExchangeRateHistoryDto> CreateHistoryAsync(string fromCurrencyCode, string toCurrencyCode, decimal rate, Abstraction.Enum.CurrencyRateProviderType providerType, DateTime rateDate, string? rawProviderPayload, CancellationToken cancellationToken = default);
    Task<ExchangeRateDto?> GetCurrentRateAsync(string fromCurrencyCode, string toCurrencyCode, CancellationToken cancellationToken = default);
    /// <summary>R1 — resolves the effective rate at <paramref name="asOfUtc"/>. Never null; HasRate=false when none.</summary>
    Task<ExchangeRateResolveDto> ResolveRateAsync(string fromCurrencyCode, string toCurrencyCode, DateTimeOffset asOfUtc, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExchangeRateHistoryDto>> GetHistoryAsync(string fromCurrencyCode, string toCurrencyCode, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExchangeRateDto>> GetRatesByCurrencyAsync(string currencyCode, CancellationToken cancellationToken = default);
}
