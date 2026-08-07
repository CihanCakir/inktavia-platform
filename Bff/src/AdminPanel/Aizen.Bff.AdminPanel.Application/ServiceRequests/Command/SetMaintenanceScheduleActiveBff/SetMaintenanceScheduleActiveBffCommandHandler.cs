using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Maintenance;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Command;

[DocumentationInfo("Set maintenance schedule active admin command handler",
    "Activates/deactivates a recurring maintenance schedule via the ServiceRequest module (S12). " +
    "The module owns the reactivate-conflict guard; its business error passes through verbatim.")]
public sealed class SetMaintenanceScheduleActiveBffCommandHandler
    : AizenCommandHandler<SetMaintenanceScheduleActiveBffCommand, SetMaintenanceScheduleActiveResponse>
{
    private readonly IServiceRequestRemoteCall _serviceRequest;

    public SetMaintenanceScheduleActiveBffCommandHandler(IServiceRequestRemoteCall serviceRequest)
        => _serviceRequest = serviceRequest;

    public override async Task<SetMaintenanceScheduleActiveResponse?> Handle(
        SetMaintenanceScheduleActiveBffCommand request, CancellationToken cancellationToken)
    {
        var result = await _serviceRequest.SetAdminMaintenanceScheduleActive(request.ScheduleId, request.Payload);
        return result.Body;
    }
}
