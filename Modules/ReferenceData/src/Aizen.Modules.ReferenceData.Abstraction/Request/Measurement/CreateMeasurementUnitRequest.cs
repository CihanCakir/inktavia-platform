using Aizen.Modules.ReferenceData.Abstraction.Enum;

namespace Aizen.Modules.ReferenceData.Abstraction.Request.Measurement;

public sealed class CreateMeasurementUnitRequest
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Symbol { get; set; } = default!;
    public MeasurementUnitType UnitType { get; set; }
    public decimal? ConversionFactorToBase { get; set; }
    public string? BaseUnitCode { get; set; }
}
