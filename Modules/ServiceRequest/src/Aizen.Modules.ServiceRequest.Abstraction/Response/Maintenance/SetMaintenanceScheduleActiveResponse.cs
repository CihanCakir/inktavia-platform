namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Maintenance;

[DocumentationInfo("Set maintenance schedule active response", "Result of an activate/deactivate toggle (S12).")]
public sealed class SetMaintenanceScheduleActiveResponse(long scheduleId, bool isActive)
{
    public long ScheduleId { get; } = scheduleId;
    public bool IsActive { get; } = isActive;
}
