using Aizen.Bff.AdminPanel.Application.Common;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;

[DocumentationInfo("Vessel engine BFF DTO", "Engine data for vessel detail response.")]
public sealed class VesselEngineBffDto
{
    public string? EngineType { get; set; }
    public string? EngineModel { get; set; }
    public string? Brand { get; set; }
    public string? PropulsionType { get; set; }
    public string? FuelType { get; set; }
    public int? HorsePower { get; set; }
    public decimal? EnginePowerKw { get; set; }
    public decimal? FuelCapacityL { get; set; }
    public decimal? MaxSpeedKnots { get; set; }
    public decimal? CruisingSpeedKnots { get; set; }
    public decimal? RangeNm { get; set; }
    public bool IsPrimary { get; set; }
}
