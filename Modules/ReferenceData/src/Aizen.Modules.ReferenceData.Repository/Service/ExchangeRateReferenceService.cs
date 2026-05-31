using Aizen.Modules.ReferenceData.Repository.Mappings;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ReferenceData.Abstraction.Dto.ExchangeRate;
using Aizen.Modules.ReferenceData.Abstraction.Enum;
using Aizen.Modules.ReferenceData.Abstraction.Request.ExchangeRate;
using Aizen.Modules.ReferenceData.Domain.Entities.ExchangeRate;
using Aizen.Modules.ReferenceData.Domain.Interface;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;
using Aizen.Modules.ReferenceData.Repository.Context;

namespace Aizen.Modules.ReferenceData.Repository.Service;

public sealed class ExchangeRateReferenceService : IExchangeRateReferenceService
{
    private readonly IExchangeRateRepository _repo;
    private readonly ReferenceDataDbContext _dbContext;

    public ExchangeRateReferenceService(IExchangeRateRepository repo, ReferenceDataDbContext dbContext)
    {
        _repo = repo;
        _dbContext = dbContext;
    }

    public async Task<ExchangeRateDto> UpdateRateAsync(UpdateExchangeRateRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _repo.GetCurrentRateAsync(request.FromCurrencyCode, request.ToCurrencyCode, cancellationToken);
        ExchangeRateEntity entity;
        if (existing != null)
        {
            existing.UpdateRate(request.Rate, request.ProviderType, request.RateDate, request.ValidUntil);
            _repo.Update(existing);
            entity = existing;
        }
        else
        {
            entity = ExchangeRateEntity.Create(request.FromCurrencyCode, request.ToCurrencyCode, request.Rate, request.ProviderType, request.RateDate, request.ValidUntil);
            await _repo.AddAsync(entity, cancellationToken);
        }

        var history = ExchangeRateHistoryEntity.Create(request.FromCurrencyCode, request.ToCurrencyCode, request.Rate, request.ProviderType, request.RateDate, request.RawProviderPayload);
        await _repo.AddHistoryAsync(history, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToDto();
    }

    public async Task SyncRatesAsync(IEnumerable<UpdateExchangeRateRequest> requests, CancellationToken cancellationToken = default)
    {
        foreach (var request in requests)
            await UpdateRateAsync(request, cancellationToken);
    }

    public async Task<ExchangeRateHistoryDto> CreateHistoryAsync(string fromCurrencyCode, string toCurrencyCode, decimal rate, CurrencyRateProviderType providerType, DateTime rateDate, string? rawProviderPayload, CancellationToken cancellationToken = default)
    {
        var entity = ExchangeRateHistoryEntity.Create(fromCurrencyCode, toCurrencyCode, rate, providerType, rateDate, rawProviderPayload);
        await _repo.AddHistoryAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToHistoryDto();
    }

    public async Task<ExchangeRateDto?> GetCurrentRateAsync(string fromCurrencyCode, string toCurrencyCode, CancellationToken cancellationToken = default)
    {
        var entity = await _repo.GetCurrentRateAsync(fromCurrencyCode, toCurrencyCode, cancellationToken);
        return entity?.ToDto();
    }

    public async Task<IReadOnlyList<ExchangeRateHistoryDto>> GetHistoryAsync(string fromCurrencyCode, string toCurrencyCode, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default)
    {
        var list = await _repo.GetHistoryAsync(fromCurrencyCode, toCurrencyCode, startDate, endDate, cancellationToken);
        return list.Select(x => x.ToHistoryDto()).ToList();
    }

    public async Task<IReadOnlyList<ExchangeRateDto>> GetRatesByCurrencyAsync(string currencyCode, CancellationToken cancellationToken = default)
    {
        var list = await _repo.GetRatesByCurrencyAsync(currencyCode, cancellationToken);
        return list.Select(x => x.ToDto()).ToList();
    }
}
