namespace Aizen.Modules.ServiceRequest.Abstraction.Request.Trip;

[DocumentationInfo("Trip location ping request", "A single provider position ping while en route.")]
public sealed class TripLocationRequest
{
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal? Heading { get; set; }
}
