using Aizen.Modules.ReferenceData.Abstraction.Dto.ExchangeRate;
using Aizen.Modules.ReferenceData.Domain.Entities.ExchangeRate;

namespace Aizen.Modules.ReferenceData.Repository.Mappings;

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

    /// <summary>
    /// R1 — maps a resolved history row (or none) to a point-in-time resolve DTO. When
    /// <paramref name="entity"/> is null, returns a clear empty result (HasRate=false) echoing
    /// the requested currencies (normalized) and as-of instant.
    /// </summary>
    public static ExchangeRateResolveDto ToResolveDto(this ExchangeRateHistoryEntity? entity, string fromCurrencyCode, string toCurrencyCode, DateTimeOffset asOfUtc)
    {
        if (entity is null)
        {
            return new ExchangeRateResolveDto
            {
                HasRate = false,
                FromCurrencyCode = fromCurrencyCode.Trim().ToUpperInvariant(),
                ToCurrencyCode = toCurrencyCode.Trim().ToUpperInvariant(),
                AsOfUtc = asOfUtc
            };
        }

        return new ExchangeRateResolveDto
        {
            HasRate = true,
            FromCurrencyCode = entity.FromCurrencyCode,
            ToCurrencyCode = entity.ToCurrencyCode,
            Rate = entity.Rate,
            ProviderType = entity.ProviderType,
            RateDate = entity.RateDate,
            AsOfUtc = asOfUtc
        };
    }
}
