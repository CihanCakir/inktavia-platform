using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Assignment;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Assignment;

namespace Aizen.Modules.ServiceRequest.Application.Command.Assignment;

[DocumentationInfo("Create assignment command", "Creates a service request assignment after offer acceptance.")]
public sealed class CreateServiceRequestAssignmentCommand : AizenCommand<CreateServiceRequestAssignmentResponse>
{
    public long ServiceRequestId { get; }
    public CreateServiceRequestAssignmentRequest Request { get; }
    public CreateServiceRequestAssignmentCommand(long serviceRequestId, CreateServiceRequestAssignmentRequest request)
    {
        ServiceRequestId = serviceRequestId; Request = request;
    }
}
