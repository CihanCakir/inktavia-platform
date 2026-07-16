using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Filter;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Provider;

namespace Aizen.Modules.ServiceRequest.Application.Query.Provider.GetProviderDiscovery;

public sealed class GetProviderDiscoveryQuery : AizenQuery<ProviderDiscoveryResponse>
{
    public ProviderServiceRequestDiscoveryFilter Filter { get; init; } = new();
}
