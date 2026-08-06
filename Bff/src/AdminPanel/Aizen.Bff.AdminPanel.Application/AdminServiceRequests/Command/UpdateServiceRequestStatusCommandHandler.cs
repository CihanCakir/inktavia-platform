using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;

[DocumentationInfo("Update service request status command handler", "Updates the status of a service request via the ServiceRequest module.")]
public sealed class UpdateServiceRequestStatusCommandHandler
    : AizenCommandHandler<UpdateServiceRequestStatusCommand, UpdateServiceRequestStatusResponse>
{
    private readonly IServiceRequestRemoteCall _serviceRequest;

    public UpdateServiceRequestStatusCommandHandler(IServiceRequestRemoteCall serviceRequest)
    {
        _serviceRequest = serviceRequest;
    }

    public override async Task<UpdateServiceRequestStatusResponse?> Handle(
        UpdateServiceRequestStatusCommand request, CancellationToken cancellationToken)
    {

        var result = await _serviceRequest.UpdateAdminServiceRequestStatus(
            request.ServiceRequestId, request.Payload);
        return result.Body;
    }
}
