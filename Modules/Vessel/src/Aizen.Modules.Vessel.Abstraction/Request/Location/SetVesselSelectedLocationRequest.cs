namespace Aizen.Modules.Vessel.Abstraction.Request.Location;

[DocumentationInfo(
    "Set vessel selected location request",
    "The owner's explicit location choice. Provide a MarinaId (with the resolved MarinaName/coords) and/or a CustomLabel and/or coordinates. An all-null body clears the selection.")]
public sealed class SetVesselSelectedLocationRequest
{
    public long? MarinaId { get; set; }
    public string? MarinaName { get; set; }
    public string? CustomLabel { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}
