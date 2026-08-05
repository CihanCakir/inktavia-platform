using Aizen.Modules.ReferenceData.Domain.Entities.ExchangeRate;
using Aizen.Modules.ReferenceData.Domain.Interface;
using Aizen.Modules.ReferenceData.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ReferenceData.Repository.Repositories.ExchangeRate;

public sealed class ExchangeRateRepository : IExchangeRateRepository
{
    private readonly ReferenceDataDbContext _dbContext;

    public ExchangeRateRepository(ReferenceDataDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ExchangeRateEntity?> GetCurrentRateAsync(string fromCurrencyCode, string toCurrencyCode, CancellationToken cancellationToken = default)
    {
        var fromCode = fromCurrencyCode.Trim().ToUpperInvariant();
        var toCode = toCurrencyCode.Trim().ToUpperInvariant();

        return _dbContext.ExchangeRates
            .FirstOrDefaultAsync(x => x.FromCurrencyCode == fromCode && x.ToCurrencyCode == toCode && x.IsActive, cancellationToken);
    }

    public async Task<IReadOnlyList<ExchangeRateEntity>> GetRatesByCurrencyAsync(string currencyCode, CancellationToken cancellationToken = default)
    {
        var code = currencyCode.Trim().ToUpperInvariant();

        return await _dbContext.ExchangeRates.AsNoTracking()
            .Where(x => x.FromCurrencyCode == code || x.ToCurrencyCode == code)
            .OrderBy(x => x.FromCurrencyCode)
            .ThenBy(x => x.ToCurrencyCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ExchangeRateHistoryEntity>> GetHistoryAsync(string fromCurrencyCode, string toCurrencyCode, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default)
    {
        var fromCode = fromCurrencyCode.Trim().ToUpperInvariant();
        var toCode = toCurrencyCode.Trim().ToUpperInvariant();

        var query = _dbContext.ExchangeRateHistories.AsNoTracking()
            .Where(x => x.FromCurrencyCode == fromCode && x.ToCurrencyCode == toCode);

        if (startDate.HasValue) query = query.Where(x => x.RateDate >= startDate.Value);
        if (endDate.HasValue) query = query.Where(x => x.RateDate <= endDate.Value);

        return await query.OrderByDescending(x => x.RateDate).ToListAsync(cancellationToken);
    }

    public Task<ExchangeRateHistoryEntity?> GetRateAsOfAsync(string fromCurrencyCode, string toCurrencyCode, DateTime asOfUtc, CancellationToken cancellationToken = default)
    {
        var fromCode = fromCurrencyCode.Trim().ToUpperInvariant();
        var toCode = toCurrencyCode.Trim().ToUpperInvariant();

        return _dbContext.ExchangeRateHistories.AsNoTracking()
            .Where(x => x.FromCurrencyCode == fromCode && x.ToCurrencyCode == toCode && x.RateDate <= asOfUtc)
            .OrderByDescending(x => x.RateDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task AddAsync(ExchangeRateEntity entity, CancellationToken cancellationToken = default)
        => _dbContext.ExchangeRates.AddAsync(entity, cancellationToken).AsTask();

    public Task AddHistoryAsync(ExchangeRateHistoryEntity entity, CancellationToken cancellationToken = default)
        => _dbContext.ExchangeRateHistories.AddAsync(entity, cancellationToken).AsTask();

    public void Update(ExchangeRateEntity entity)
        => _dbContext.ExchangeRates.Update(entity);
}
