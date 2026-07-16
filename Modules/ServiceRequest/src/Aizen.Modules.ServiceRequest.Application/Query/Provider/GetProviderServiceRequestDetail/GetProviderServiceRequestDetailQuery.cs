using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Modules.ServiceRequest.Application.Query.Provider.GetProviderServiceRequestDetail;

/// <summary>
/// A single service request, read by a provider.
///
/// The provider id is deliberately NOT a parameter — it comes from the trusted request context (the BFF asserts it
/// via X-Aizen-Provider-Profile-Id). Accepting it from the caller would let anyone read any request "as" any
/// provider, which is the whole point of the access check in the handler.
/// </summary>
[DocumentationInfo("Get provider service request detail query", "Returns one service request, if the calling provider is allowed to see it.")]
public sealed class GetProviderServiceRequestDetailQuery : AizenQuery<GetProviderServiceRequestDetailResponse>
{
    public long ServiceRequestId { get; }

    public GetProviderServiceRequestDetailQuery(long serviceRequestId)
    {
        ServiceRequestId = serviceRequestId;
    }
}
