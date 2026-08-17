using Aizen.Modules.Vessel.Abstraction.Enum;

namespace Aizen.Modules.Vessel.Abstraction.Request.Vessel;

[DocumentationInfo("Filter vessels request", "Filter and search input for querying vessel lists.")]
public sealed class FilterVesselsRequest
{
    public string? NameContains { get; set; }
    public string? VesselTypeCode { get; set; }
    public string? FlagCountryCode { get; set; }
    public VesselStatus? Status { get; set; }
    public VesselVisibility? Visibility { get; set; }
    public bool? IsArchived { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
