using Aizen.Modules.ReferenceData.Abstraction.Dto.ExchangeRate;
using Aizen.Modules.ReferenceData.Domain.Entities.ExchangeRate;

namespace Aizen.Modules.ReferenceData.Application.ExchangeRate.Mappings;

public static class ExchangeRateMappingExtensions
{
    public static ExchangeRateDto ToDto(this ExchangeRateEntity entity) => new()
    {
        Id = entity.Id,
        FromCurrencyCode = entity.FromCurrencyCode,
        ToCurrencyCode = entity.ToCurrencyCode,
        Rate = entity.Rate,
        ProviderType = entity.ProviderType,
        RateDate = entity.RateDate,
        ValidUntil = entity.ValidUntil,
        IsActive = entity.IsActive
    };

    public static ExchangeRateHistoryDto ToHistoryDto(this ExchangeRateHistoryEntity entity) => new()
    {
        Id = entity.Id,
        FromCurrencyCode = entity.FromCurrencyCode,
        ToCurrencyCode = entity.ToCurrencyCode,
        Rate = entity.Rate,
        ProviderType = entity.ProviderType,
        RateDate = entity.RateDate
    };
}
