using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Provider;

namespace Aizen.Modules.ServiceRequest.Application.Query.Provider.GetOpenServiceRequests;

[DocumentationInfo("Get open service requests query", "Returns biddable service requests for a provider, excluding those already bid on.")]
public sealed class GetOpenServiceRequestsQuery : AizenQuery<GetOpenServiceRequestsResponse>
{
    public int PageIndex { get; }
    public int PageSize { get; }
    public string? ServiceCategoryCode { get; init; }
    public string? LocationCityCode { get; init; }
    public string? LocationCountryCode { get; init; }
    public ServiceRequestPriority? MinPriority { get; init; }
    public string? SearchTerm { get; init; }

    public GetOpenServiceRequestsQuery(int pageIndex = 0, int pageSize = 20)
    {
        PageIndex = pageIndex < 0 ? 0 : pageIndex;
        PageSize = pageSize is <= 0 or > 100 ? 20 : pageSize;
    }
}
