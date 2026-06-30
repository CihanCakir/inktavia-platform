using Aizen.Core.Domain;

namespace Aizen.Modules.Vessel.Domain.Entities.Vessel;

[DocumentationInfo("Vessel specification entity", "Physical measurements and technical specifications of a vessel.")]
public sealed class VesselSpecificationEntity : AizenEntityWithAudit
{
    public long VesselId { get; private set; }
    public string? Brand { get; private set; }
    public string? Model { get; private set; }
    public int? ProductionYear { get; private set; }
    public decimal? LengthValue { get; private set; }
    public string? LengthUnitCode { get; private set; }
    public decimal? BeamValue { get; private set; }
    public string? BeamUnitCode { get; private set; }
    public decimal? DraftValue { get; private set; }
    public string? DraftUnitCode { get; private set; }
    public decimal? WeightValue { get; private set; }
    public string? WeightUnitCode { get; private set; }
    public int? CabinCount { get; private set; }
    public int? BedCount { get; private set; }
    public int? BathroomCount { get; private set; }
    public string? HullMaterialCode { get; private set; }
    public decimal? FuelCapacityValue { get; private set; }
    public string? FuelCapacityUnitCode { get; private set; }
    public decimal? WaterCapacityValue { get; private set; }
    public string? WaterCapacityUnitCode { get; private set; }
    public string? BuildCountry { get; private set; }
    public string? SuperstructureMaterial { get; private set; }
    public decimal? GrossTonnage { get; private set; }
    public decimal? NetTonnage { get; private set; }
    public int? PassengerCapacity { get; private set; }
    public int? CrewCapacity { get; private set; }

    public VesselEntity? Vessel { get; private set; }

    public VesselSpecificationEntity() { }

    public static VesselSpecificationEntity Create(
        long vesselId,
        string? brand, string? model, int? productionYear,
        decimal? lengthValue, string? lengthUnitCode,
        decimal? beamValue, string? beamUnitCode,
        decimal? draftValue, string? draftUnitCode,
        decimal? weightValue, string? weightUnitCode,
        int? cabinCount, int? bedCount, int? bathroomCount,
        string? hullMaterialCode,
        decimal? fuelCapacityValue, string? fuelCapacityUnitCode,
        decimal? waterCapacityValue, string? waterCapacityUnitCode)
    {
        return new VesselSpecificationEntity
        {
            VesselId = vesselId,
            Brand = brand, Model = model, ProductionYear = productionYear,
            LengthValue = lengthValue, LengthUnitCode = lengthUnitCode?.ToUpperInvariant(),
            BeamValue = beamValue, BeamUnitCode = beamUnitCode?.ToUpperInvariant(),
            DraftValue = draftValue, DraftUnitCode = draftUnitCode?.ToUpperInvariant(),
            WeightValue = weightValue, WeightUnitCode = weightUnitCode?.ToUpperInvariant(),
            CabinCount = cabinCount, BedCount = bedCount, BathroomCount = bathroomCount,
            HullMaterialCode = hullMaterialCode?.ToUpperInvariant(),
            FuelCapacityValue = fuelCapacityValue, FuelCapacityUnitCode = fuelCapacityUnitCode?.ToUpperInvariant(),
            WaterCapacityValue = waterCapacityValue, WaterCapacityUnitCode = waterCapacityUnitCode?.ToUpperInvariant(),
            IsActive = true
        };
    }

    public void Update(
        string? brand, string? model, int? productionYear,
        decimal? lengthValue, string? lengthUnitCode,
        decimal? beamValue, string? beamUnitCode,
        decimal? draftValue, string? draftUnitCode,
        decimal? weightValue, string? weightUnitCode,
        int? cabinCount, int? bedCount, int? bathroomCount,
        string? hullMaterialCode,
        decimal? fuelCapacityValue, string? fuelCapacityUnitCode,
        decimal? waterCapacityValue, string? waterCapacityUnitCode)
    {
        Brand = brand; Model = model; ProductionYear = productionYear;
        LengthValue = lengthValue; LengthUnitCode = lengthUnitCode?.ToUpperInvariant();
        BeamValue = beamValue; BeamUnitCode = beamUnitCode?.ToUpperInvariant();
        DraftValue = draftValue; DraftUnitCode = draftUnitCode?.ToUpperInvariant();
        WeightValue = weightValue; WeightUnitCode = weightUnitCode?.ToUpperInvariant();
        CabinCount = cabinCount; BedCount = bedCount; BathroomCount = bathroomCount;
        HullMaterialCode = hullMaterialCode?.ToUpperInvariant();
        FuelCapacityValue = fuelCapacityValue; FuelCapacityUnitCode = fuelCapacityUnitCode?.ToUpperInvariant();
        WaterCapacityValue = waterCapacityValue; WaterCapacityUnitCode = waterCapacityUnitCode?.ToUpperInvariant();
    }
}
