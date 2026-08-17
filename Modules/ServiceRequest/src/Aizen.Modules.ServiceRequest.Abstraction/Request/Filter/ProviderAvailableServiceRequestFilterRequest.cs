using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Request.Filter;

[DocumentationInfo("Provider available service request filter", "Filter parameters for providers browsing open service requests.")]
public sealed class ProviderAvailableServiceRequestFilterRequest
{
    public int PageIndex { get; set; } = 0;
    public int PageSize { get; set; } = 20;
    public string? ServiceCategoryCode { get; set; }
    public string? LocationCityCode { get; set; }
    public string? LocationCountryCode { get; set; }
    public ServiceRequestPriority? MinPriority { get; set; }
    public string? SearchTerm { get; set; }
}
