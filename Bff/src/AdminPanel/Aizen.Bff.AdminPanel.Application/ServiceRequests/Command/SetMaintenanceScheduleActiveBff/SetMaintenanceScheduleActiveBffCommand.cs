using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Maintenance;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Maintenance;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Command;

public sealed class SetMaintenanceScheduleActiveBffCommand : AizenCommand<SetMaintenanceScheduleActiveResponse>
{
    public long ScheduleId { get; }
    public SetMaintenanceScheduleActiveRequest Payload { get; }

    public SetMaintenanceScheduleActiveBffCommand(long scheduleId, SetMaintenanceScheduleActiveRequest payload)
    {
        ScheduleId = scheduleId;
        Payload    = payload;
    }
}
