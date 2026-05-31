using Aizen.Core.Domain;
using Aizen.Modules.ReferenceData.Abstraction.Enum;

namespace Aizen.Modules.ReferenceData.Domain.Entities.Measurement;

public sealed class MeasurementUnitEntity : AizenEntityWithAudit
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string Symbol { get; private set; } = default!;
    public MeasurementUnitType UnitType { get; private set; }
    public decimal? ConversionFactorToBase { get; private set; }
    public string? BaseUnitCode { get; private set; }

    private MeasurementUnitEntity() { }

    public static MeasurementUnitEntity Create(string code, string name, string symbol, MeasurementUnitType unitType, decimal? conversionFactorToBase, string? baseUnitCode)
    {
        return new MeasurementUnitEntity
        {
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Symbol = symbol.Trim(),
            UnitType = unitType,
            ConversionFactorToBase = conversionFactorToBase,
            BaseUnitCode = baseUnitCode?.Trim().ToUpperInvariant(),
            IsActive = true
        };
    }

    public void Update(string name, string symbol, decimal? conversionFactorToBase, string? baseUnitCode, bool isActive)
    {
        Name = name.Trim();
        Symbol = symbol.Trim();
        ConversionFactorToBase = conversionFactorToBase;
        BaseUnitCode = baseUnitCode?.Trim().ToUpperInvariant();
        IsActive = isActive;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
