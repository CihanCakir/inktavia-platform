using Aizen.Modules.ReferenceData.Domain.Entities.ExchangeRate;

namespace Aizen.Modules.ReferenceData.Domain.Interface;

public interface IExchangeRateRepository
{
    Task<ExchangeRateEntity?> GetCurrentRateAsync(string fromCurrencyCode, string toCurrencyCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExchangeRateEntity>> GetRatesByCurrencyAsync(string currencyCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExchangeRateHistoryEntity>> GetHistoryAsync(string fromCurrencyCode, string toCurrencyCode, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default);
    Task AddAsync(ExchangeRateEntity entity, CancellationToken cancellationToken = default);
    Task AddHistoryAsync(ExchangeRateHistoryEntity entity, CancellationToken cancellationToken = default);
    void Update(ExchangeRateEntity entity);
}
