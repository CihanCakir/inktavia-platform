using Aizen.Modules.ReferenceData.Abstraction.Dto.Currency;
using Aizen.Modules.ReferenceData.Abstraction.Request.Currency;

namespace Aizen.Modules.ReferenceData.Domain.Interface.Service;

public interface ICurrencyReferenceService
{
    Task<CurrencyDto> CreateAsync(CreateCurrencyRequest request, CancellationToken cancellationToken = default);
    Task<CurrencyDto> UpdateAsync(long id, string name, string symbol, int decimalPlaces, bool isActive, CancellationToken cancellationToken = default);
    Task ActivateAsync(long id, CancellationToken cancellationToken = default);
    Task DeactivateAsync(long id, CancellationToken cancellationToken = default);
    Task<CurrencyDto> SetBaseCurrencyAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CurrencyDto>> GetListAsync(bool onlyActive, CancellationToken cancellationToken = default);
    Task<CurrencyDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<CurrencyDto?> GetBaseCurrencyAsync(CancellationToken cancellationToken = default);
}
