using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Modules.ServiceRequest.Application.Command.ServiceRequest;

[DocumentationInfo("Complete service request command", "Admin marks a service request as completed.")]
public sealed class CompleteServiceRequestCommand : AizenCommand<CompleteServiceRequestResponse>
{
    public long ServiceRequestId { get; }
    public CompleteServiceRequestCommand(long serviceRequestId) => ServiceRequestId = serviceRequestId;
}
