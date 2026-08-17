using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.WorkLog;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;

[DocumentationInfo("Update work phase admin command handler", "Updates a work phase's progress and status via the ServiceRequest module.")]
public sealed class UpdateWorkPhaseAdminCommandHandler
    : AizenCommandHandler<UpdateWorkPhaseAdminCommand, UpdateWorkPhaseResponse>
{
    private readonly IServiceRequestAdminBffRemoteCall _serviceRequest;

    public UpdateWorkPhaseAdminCommandHandler(IServiceRequestAdminBffRemoteCall serviceRequest)
    {
        _serviceRequest = serviceRequest;
    }

    public override async Task<UpdateWorkPhaseResponse?> Handle(
        UpdateWorkPhaseAdminCommand request, CancellationToken cancellationToken)
    {

        var result = await _serviceRequest.UpdateAdminWorkPhase(
            request.ServiceRequestId, request.PhaseNumber, request.Payload);
        return result.Body;
    }
}
