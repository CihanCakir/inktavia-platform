using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Command;

[DocumentationInfo("Assign service request provider command handler", "Assigns a provider to a service request via the ServiceRequest module.")]
public sealed class AssignServiceRequestProviderBffCommandHandler
    : AizenCommandHandler<AssignServiceRequestProviderBffCommand, AssignProviderResponse>
{
    private readonly IServiceRequestRemoteCall _serviceRequest;

    public AssignServiceRequestProviderBffCommandHandler(IServiceRequestRemoteCall serviceRequest)
    {
        _serviceRequest = serviceRequest;
    }

    public override async Task<AssignProviderResponse?> Handle(
        AssignServiceRequestProviderBffCommand request, CancellationToken cancellationToken)
    {

        var result = await _serviceRequest.AssignServiceRequestProvider(
            request.ServiceRequestId, request.Payload);
        return result.Body;
    }
}
