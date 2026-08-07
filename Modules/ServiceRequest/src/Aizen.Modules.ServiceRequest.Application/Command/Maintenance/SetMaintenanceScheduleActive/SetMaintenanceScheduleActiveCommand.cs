using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Maintenance;

namespace Aizen.Modules.ServiceRequest.Application.Command.Maintenance;

[DocumentationInfo("Set maintenance schedule active command", "Activate or deactivate a maintenance schedule (S12).")]
public sealed class SetMaintenanceScheduleActiveCommand : AizenCommand<SetMaintenanceScheduleActiveResponse>
{
    public long ScheduleId { get; }
    public bool IsActive { get; }

    public SetMaintenanceScheduleActiveCommand(long scheduleId, bool isActive)
    {
        ScheduleId = scheduleId;
        IsActive   = isActive;
    }
}
