using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Maintenance;

namespace Aizen.Modules.ServiceRequest.Application.Command.Maintenance;

/// <summary>
/// BE-MO8 — owner activate/deactivate. Same behaviour as the admin <c>SetMaintenanceScheduleActive</c> (idempotent
/// no-op + reactivate-conflict guard) but owner-gated: the schedule's <c>OwnerUserId</c> must equal the caller (from
/// the token), else a clean not-found. The admin command/handler are untouched.
/// </summary>
[DocumentationInfo("Set owner maintenance schedule active command", "Owner-gated activate/deactivate of the caller's own schedule (MO8).")]
public sealed class SetOwnerMaintenanceScheduleActiveCommand : AizenCommand<SetMaintenanceScheduleActiveResponse>
{
    public long OwnerUserId { get; }
    public long ScheduleId { get; }
    public bool IsActive { get; }

    public SetOwnerMaintenanceScheduleActiveCommand(long ownerUserId, long scheduleId, bool isActive)
    {
        OwnerUserId = ownerUserId;
        ScheduleId  = scheduleId;
        IsActive    = isActive;
    }
}
