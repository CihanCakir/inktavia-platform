using Aizen.Modules.ReferenceData.Abstraction.Enum;

namespace Aizen.Modules.ReferenceData.Abstraction.Dto.Measurement;

public sealed class MeasurementUnitDto
{
    public long Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Symbol { get; set; } = default!;
    public MeasurementUnitType UnitType { get; set; }
    public decimal? ConversionFactorToBase { get; set; }
    public string? BaseUnitCode { get; set; }
    public bool IsActive { get; set; }
}
