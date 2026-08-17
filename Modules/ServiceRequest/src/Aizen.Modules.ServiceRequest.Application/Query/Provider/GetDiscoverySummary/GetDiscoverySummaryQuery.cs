using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Filter;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Provider;

namespace Aizen.Modules.ServiceRequest.Application.Query.Provider.GetDiscoverySummary;

public sealed class GetDiscoverySummaryQuery : AizenQuery<ProviderDiscoverySummaryResponse>
{
    public ProviderServiceRequestDiscoveryFilter Filter { get; init; } = new();
}
