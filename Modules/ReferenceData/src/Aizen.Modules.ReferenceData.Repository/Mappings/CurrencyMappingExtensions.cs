using Aizen.Modules.ReferenceData.Abstraction.Dto.Currency;
using Aizen.Modules.ReferenceData.Domain.Entities.Currency;

namespace Aizen.Modules.ReferenceData.Repository.Mappings;

public static class CurrencyMappingExtensions
{
    public static CurrencyDto ToDto(this CurrencyEntity entity) => new()
    {
        Id = entity.Id,
        Code = entity.Code,
        NumericCode = entity.NumericCode,
        Name = entity.Name,
        Symbol = entity.Symbol,
        DecimalPlaces = entity.DecimalPlaces,
        IsBaseCurrency = entity.IsBaseCurrency,
        IsActive = entity.IsActive
    };
}
