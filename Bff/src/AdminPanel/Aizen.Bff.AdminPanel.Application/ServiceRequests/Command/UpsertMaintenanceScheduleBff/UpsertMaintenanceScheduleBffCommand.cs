using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Maintenance;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Maintenance;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Command;

public sealed class UpsertMaintenanceScheduleBffCommand : AizenCommand<UpsertMaintenanceScheduleResponse>
{
    public UpsertMaintenanceScheduleRequest Payload { get; }

    public UpsertMaintenanceScheduleBffCommand(UpsertMaintenanceScheduleRequest payload)
    {
        Payload = payload;
    }
}
