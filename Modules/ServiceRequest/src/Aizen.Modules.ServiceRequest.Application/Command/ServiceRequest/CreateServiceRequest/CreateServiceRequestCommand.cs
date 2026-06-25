using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Modules.ServiceRequest.Application.Command.ServiceRequest;

[DocumentationInfo("Create service request command", "Carries the payload to create a new service request.")]
public sealed class CreateServiceRequestCommand : AizenCommand<CreateServiceRequestResponse>
{
    public CreateServiceRequestRequest Request { get; }
    public CreateServiceRequestCommand(CreateServiceRequestRequest request) => Request = request;
}
