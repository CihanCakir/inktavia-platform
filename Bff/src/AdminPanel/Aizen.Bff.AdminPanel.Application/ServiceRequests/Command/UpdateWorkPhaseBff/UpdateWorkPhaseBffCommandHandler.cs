using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.WorkLog;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Command;

[DocumentationInfo("Update work phase admin command handler", "Updates a work phase's progress and status via the ServiceRequest module.")]
public sealed class UpdateWorkPhaseBffCommandHandler
    : AizenCommandHandler<UpdateWorkPhaseBffCommand, UpdateWorkPhaseResponse>
{
    private readonly IServiceRequestRemoteCall _serviceRequest;

    public UpdateWorkPhaseBffCommandHandler(IServiceRequestRemoteCall serviceRequest)
    {
        _serviceRequest = serviceRequest;
    }

    public override async Task<UpdateWorkPhaseResponse?> Handle(
        UpdateWorkPhaseBffCommand request, CancellationToken cancellationToken)
    {

        var result = await _serviceRequest.UpdateAdminWorkPhase(
            request.ServiceRequestId, request.PhaseNumber, request.Payload);
        return result.Body;
    }
}
