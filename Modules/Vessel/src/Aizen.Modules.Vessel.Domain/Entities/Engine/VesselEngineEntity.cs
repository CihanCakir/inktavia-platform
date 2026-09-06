using Aizen.Core.Domain;

namespace Aizen.Modules.Vessel.Domain.Entities.Vessel;

[DocumentationInfo("Vessel engine entity", "Represents a propulsion engine installed on a vessel.")]
public sealed class VesselEngineEntity : AizenEntityWithAudit
{
    public long VesselId { get; private set; }
    public string EngineName { get; private set; } = default!;
    public string EngineTypeCode { get; private set; } = default!;
    public string FuelTypeCode { get; private set; } = default!;
    public string? Brand { get; private set; }
    public string? Model { get; private set; }
    /// <summary>Optional link to the ReferenceData engine brand/model catalog. Brand/Model stay as the denormalized
    /// display fallback so nothing downstream depends on the catalog.</summary>
    public long? EngineBrandId { get; private set; }
    public long? EngineModelId { get; private set; }
    public string? SerialNumber { get; private set; }
    public int? HorsePower { get; private set; }
    public int? ProductionYear { get; private set; }
    public bool IsPrimary { get; private set; }
    public string? PropulsionType { get; private set; }
    public decimal? EnginePowerKw { get; private set; }
    public decimal? FuelCapacityL { get; private set; }
    public decimal? MaxSpeedKnots { get; private set; }
    public decimal? CruisingSpeedKnots { get; private set; }
    public decimal? RangeNm { get; private set; }

    public VesselEntity? Vessel { get; private set; }

    public VesselEngineEntity() { }

    public static VesselEngineEntity Create(
        long vesselId, string engineName, string engineTypeCode, string fuelTypeCode,
        string? brand, string? model, string? serialNumber, int? horsePower, int? productionYear, bool isPrimary)
    {
        return new VesselEngineEntity
        {
            VesselId = vesselId,
            EngineName = engineName.Trim(),
            EngineTypeCode = engineTypeCode.ToUpperInvariant(),
            FuelTypeCode = fuelTypeCode.ToUpperInvariant(),
            Brand = brand,
            Model = model,
            SerialNumber = serialNumber,
            HorsePower = horsePower,
            ProductionYear = productionYear,
            IsPrimary = isPrimary,
            IsActive = true
        };
    }

    public void Update(string engineName, string engineTypeCode, string fuelTypeCode,
        string? brand, string? model, string? serialNumber, int? horsePower, int? productionYear)
    {
        EngineName = engineName.Trim();
        EngineTypeCode = engineTypeCode.ToUpperInvariant();
        FuelTypeCode = fuelTypeCode.ToUpperInvariant();
        Brand = brand;
        Model = model;
        SerialNumber = serialNumber;
        HorsePower = horsePower;
        ProductionYear = productionYear;
    }

    /// <summary>Set/clear the catalog brand+model link (nullable = free-text only).</summary>
    public void SetBrandModel(long? engineBrandId, long? engineModelId)
    {
        EngineBrandId = engineBrandId;
        EngineModelId = engineModelId;
    }

    public void SetPrimary() => IsPrimary = true;
    public void ClearPrimary() => IsPrimary = false;
    public void Deactivate() => IsActive = false;
}
