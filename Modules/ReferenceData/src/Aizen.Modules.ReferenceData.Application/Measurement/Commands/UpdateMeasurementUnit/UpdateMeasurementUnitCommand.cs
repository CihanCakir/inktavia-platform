using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Measurement;

namespace Aizen.Modules.ReferenceData.Application.Measurement.Commands;

public sealed class UpdateMeasurementUnitCommand : AizenCommand<MeasurementUnitDto>
{
    public long Id { get; }
    public string Name { get; }
    public string Symbol { get; }
    public decimal? ConversionFactorToBase { get; }
    public string? BaseUnitCode { get; }
    public bool IsActive { get; }

    public UpdateMeasurementUnitCommand(long id, string name, string symbol, decimal? conversionFactorToBase, string? baseUnitCode, bool isActive)
    {
        Id = id;
        Name = name;
        Symbol = symbol;
        ConversionFactorToBase = conversionFactorToBase;
        BaseUnitCode = baseUnitCode;
        IsActive = isActive;
    }
}
