
namespace Aizen.Modules.ServiceRequest.Abstraction.Dto;

[DocumentationInfo("ServiceRequest location snapshot DTO", "Snapshot of the location associated with a service request.")]
public sealed class ServiceRequestLocationSnapshotDto
{
    public string? CountryCode { get; set; }
    public string? CityCode { get; set; }
    public string? MarinaName { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}
