namespace Aizen.Modules.Vessel.Abstraction.Dto.Location;

[DocumentationInfo("Vessel selected location DTO", "The owner's explicit location choice: a marina reference (with denormalized name/coords) and/or a free-text label. Preferred over the current-location snapshot for display.")]
public sealed class VesselSelectedLocationDto
{
    public long? MarinaId { get; set; }
    public string? MarinaName { get; set; }
    public string? CustomLabel { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public DateTime? SetAt { get; set; }
}
