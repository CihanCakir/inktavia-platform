using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Maintenance;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Command;

[DocumentationInfo("Upsert maintenance schedule admin command handler",
    "Creates or updates (idempotent by vessel+category[+type]) a recurring maintenance schedule via the ServiceRequest module (S12).")]
public sealed class UpsertMaintenanceScheduleBffCommandHandler
    : AizenCommandHandler<UpsertMaintenanceScheduleBffCommand, UpsertMaintenanceScheduleResponse>
{
    private readonly IServiceRequestRemoteCall _serviceRequest;

    public UpsertMaintenanceScheduleBffCommandHandler(IServiceRequestRemoteCall serviceRequest)
    {
        _serviceRequest = serviceRequest;
    }

    public override async Task<UpsertMaintenanceScheduleResponse?> Handle(
        UpsertMaintenanceScheduleBffCommand request, CancellationToken cancellationToken)
    {
        var result = await _serviceRequest.UpsertAdminMaintenanceSchedule(request.Payload);
        return result.Body;
    }
}
