using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Filter;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Provider;

namespace Aizen.Modules.ServiceRequest.Application.Query.Provider.GetDiscoveryMarkers;

public sealed class GetDiscoveryMarkersQuery : AizenQuery<ProviderDiscoveryMarkersResponse>
{
    public ProviderServiceRequestDiscoveryFilter Filter { get; init; } = new();
}
