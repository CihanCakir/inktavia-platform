using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;

[DocumentationInfo("Complete service request admin command handler", "Marks a service request as completed via the ServiceRequest module.")]
public sealed class CompleteServiceRequestAdminCommandHandler
    : AizenCommandHandler<CompleteServiceRequestAdminCommand, CompleteServiceRequestResponse>
{
    private readonly IServiceRequestAdminBffRemoteCall _serviceRequest;

    public CompleteServiceRequestAdminCommandHandler(IServiceRequestAdminBffRemoteCall serviceRequest)
    {
        _serviceRequest = serviceRequest;
    }

    public override async Task<CompleteServiceRequestResponse?> Handle(
        CompleteServiceRequestAdminCommand request, CancellationToken cancellationToken)
    {

        var result = await _serviceRequest.CompleteServiceRequest(
            request.ServiceRequestId);
        return result.Body;
    }
}
