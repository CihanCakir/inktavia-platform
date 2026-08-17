using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Provider;

namespace Aizen.Bff.MarineProvider.Application.ServiceRequests;

public sealed class GetOpenServiceRequestsBffQuery : AizenQuery<GetOpenServiceRequestsResponse>
{
    public int PageIndex { get; init; }
    public int PageSize { get; init; } = 20;
    public string? ServiceCategoryCode { get; init; }
    public string? LocationCityCode { get; init; }
    public string? LocationCountryCode { get; init; }
    public string? SearchTerm { get; init; }
}
