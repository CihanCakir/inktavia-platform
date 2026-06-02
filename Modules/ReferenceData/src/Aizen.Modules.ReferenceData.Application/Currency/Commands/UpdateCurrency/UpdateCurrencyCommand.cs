using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Currency;

namespace Aizen.Modules.ReferenceData.Application.Currency.Commands;

public sealed class UpdateCurrencyCommand : AizenCommand<CurrencyDto>
{
    public long Id { get; }
    public string Name { get; }
    public string Symbol { get; }
    public int DecimalPlaces { get; }
    public bool IsActive { get; }

    public UpdateCurrencyCommand(long id, string name, string symbol, int decimalPlaces, bool isActive)
    {
        Id = id;
        Name = name;
        Symbol = symbol;
        DecimalPlaces = decimalPlaces;
        IsActive = isActive;
    }
}
