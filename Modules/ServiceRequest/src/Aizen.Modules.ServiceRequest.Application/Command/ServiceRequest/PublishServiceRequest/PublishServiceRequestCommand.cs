using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Modules.ServiceRequest.Application.Command.ServiceRequest;

[DocumentationInfo("Publish service request command", "Transitions a draft service request to Open status, making it visible to providers.")]
public sealed class PublishServiceRequestCommand : AizenCommand<UpdateServiceRequestResponse>
{
    public long ServiceRequestId { get; }
    public PublishServiceRequestCommand(long serviceRequestId) => ServiceRequestId = serviceRequestId;
}
