namespace Aizen.Modules.ReferenceData.Abstraction.Request.Measurement;

public sealed class UpdateMeasurementUnitRequest
{
    public string Name { get; set; } = default!;
    public string Symbol { get; set; } = default!;
    public decimal? ConversionFactorToBase { get; set; }
    public string? BaseUnitCode { get; set; }
    public bool IsActive { get; set; }
}
