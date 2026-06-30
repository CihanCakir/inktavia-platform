using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Message;

namespace Aizen.Modules.ServiceRequest.Application.Query.Message;

[DocumentationInfo("Get messages query", "Returns paginated messages for a service request thread.")]
public sealed class GetServiceRequestMessagesQuery : AizenQuery<GetServiceRequestMessagesResponse>
{
    public long ServiceRequestId { get; }
    public int Skip { get; }
    public int Take { get; }
    public GetServiceRequestMessagesQuery(long serviceRequestId, int skip, int take)
    {
        ServiceRequestId = serviceRequestId; Skip = skip; Take = take;
    }
}
