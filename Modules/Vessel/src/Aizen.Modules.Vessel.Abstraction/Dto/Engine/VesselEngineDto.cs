using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Abstraction.Dto.Engine;

[DocumentationInfo("Vessel engine DTO", "Engine details including type, fuel, and manufacturer info.")]
public sealed class VesselEngineDto
{
    public long Id { get; set; }
    public long VesselId { get; set; }
    public string EngineName { get; set; } = default!;
    public string EngineTypeCode { get; set; } = default!;
    public string FuelTypeCode { get; set; } = default!;
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public int? HorsePower { get; set; }
    public int? ProductionYear { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; }
}
