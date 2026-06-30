
namespace Aizen.Modules.ReferenceData.Repository.Seed.Models.Measurement;

/// <summary>Seed model for a MeasurementUnit entity, read from measurement-units.json.</summary>
[DocumentationInfo("Seed model representing a measurement unit loaded from JSON.", "Maps to MeasurementUnitEntity. Idempotency key: Code. UnitType maps to MeasurementUnitType enum integer.")]
public sealed class MeasurementUnitSeedModel
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Symbol { get; set; } = default!;
    public int UnitType { get; set; }
    public decimal? ConversionFactorToBase { get; set; }
    public string? BaseUnitCode { get; set; }
    public bool IsActive { get; set; } = true;
}
