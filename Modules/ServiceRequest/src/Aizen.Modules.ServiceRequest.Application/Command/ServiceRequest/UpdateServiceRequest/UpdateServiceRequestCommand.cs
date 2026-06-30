using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Modules.ServiceRequest.Application.Command.ServiceRequest;

[DocumentationInfo("Update service request command", "Updates profile fields of an existing service request.")]
public sealed class UpdateServiceRequestCommand : AizenCommand<UpdateServiceRequestResponse>
{
    public long ServiceRequestId { get; }
    public UpdateServiceRequestRequest Request { get; }
    public UpdateServiceRequestCommand(long serviceRequestId, UpdateServiceRequestRequest request)
    {
        ServiceRequestId = serviceRequestId;
        Request = request;
    }
}
