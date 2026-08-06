using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Command;

[DocumentationInfo("Complete service request admin command handler", "Marks a service request as completed via the ServiceRequest module.")]
public sealed class CompleteServiceRequestBffCommandHandler
    : AizenCommandHandler<CompleteServiceRequestBffCommand, CompleteServiceRequestResponse>
{
    private readonly IServiceRequestRemoteCall _serviceRequest;

    public CompleteServiceRequestBffCommandHandler(IServiceRequestRemoteCall serviceRequest)
    {
        _serviceRequest = serviceRequest;
    }

    public override async Task<CompleteServiceRequestResponse?> Handle(
        CompleteServiceRequestBffCommand request, CancellationToken cancellationToken)
    {

        var result = await _serviceRequest.CompleteServiceRequest(
            request.ServiceRequestId);
        return result.Body;
    }
}
