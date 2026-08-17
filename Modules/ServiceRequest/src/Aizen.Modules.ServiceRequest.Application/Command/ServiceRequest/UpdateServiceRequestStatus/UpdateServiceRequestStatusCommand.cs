using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Modules.ServiceRequest.Application.Command.ServiceRequest;

[DocumentationInfo("Update service request status command", "Admin status change on a service request.")]
public sealed class UpdateServiceRequestStatusCommand : AizenCommand<UpdateServiceRequestStatusResponse>
{
    public long ServiceRequestId { get; }
    public UpdateServiceRequestStatusRequest Request { get; }
    public UpdateServiceRequestStatusCommand(long serviceRequestId, UpdateServiceRequestStatusRequest request)
    {
        ServiceRequestId = serviceRequestId; Request = request;
    }
}
