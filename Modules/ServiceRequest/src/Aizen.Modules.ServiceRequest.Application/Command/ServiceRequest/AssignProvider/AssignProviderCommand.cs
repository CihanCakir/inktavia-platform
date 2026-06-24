using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Modules.ServiceRequest.Application.Command.ServiceRequest;

[DocumentationInfo("Assign provider command", "Assigns a provider to a service request.")]
public sealed class AssignProviderCommand : AizenCommand<AssignProviderResponse>
{
    public long ServiceRequestId { get; }
    public AssignProviderRequest Request { get; }
    public AssignProviderCommand(long serviceRequestId, AssignProviderRequest request)
    {
        ServiceRequestId = serviceRequestId; Request = request;
    }
}
