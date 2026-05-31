using Aizen.Core.Domain;

namespace Aizen.Modules.ReferenceData.Domain.Entities.Currency;

public sealed class CurrencyEntity : AizenEntityWithAudit
{
    public string Code { get; private set; } = default!;
    public string NumericCode { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string Symbol { get; private set; } = default!;
    public int DecimalPlaces { get; private set; }
    public bool IsBaseCurrency { get; private set; }

    private CurrencyEntity() { }

    public static CurrencyEntity Create(string code, string numericCode, string name, string symbol, int decimalPlaces, bool isBaseCurrency)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Currency code is required.", nameof(code));
        if (code.Trim().Length != 3) throw new ArgumentException("Currency code must be 3 characters.", nameof(code));
        if (decimalPlaces < 0 || decimalPlaces > 6) throw new ArgumentOutOfRangeException(nameof(decimalPlaces));

        return new CurrencyEntity
        {
            Code = code.Trim().ToUpperInvariant(),
            NumericCode = numericCode.Trim(),
            Name = name.Trim(),
            Symbol = symbol.Trim(),
            DecimalPlaces = decimalPlaces,
            IsBaseCurrency = isBaseCurrency,
            IsActive = true
        };
    }

    public void Update(string name, string symbol, int decimalPlaces, bool isActive)
    {
        if (decimalPlaces < 0 || decimalPlaces > 6) throw new ArgumentOutOfRangeException(nameof(decimalPlaces));
        Name = name.Trim();
        Symbol = symbol.Trim();
        DecimalPlaces = decimalPlaces;
        IsActive = isActive;
    }

    public void MarkAsBaseCurrency() => IsBaseCurrency = true;
    public void UnmarkAsBaseCurrency() => IsBaseCurrency = false;
    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
