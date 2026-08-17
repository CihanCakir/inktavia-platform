using Aizen.Modules.ServiceRequest.Abstraction.Dto;

namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Maintenance;

[DocumentationInfo("Maintenance schedule list response", "Active maintenance schedules for admin management (S12).")]
public sealed class GetMaintenanceScheduleListResponse(List<MaintenanceScheduleDto> schedules)
{
    public List<MaintenanceScheduleDto> Schedules { get; } = schedules;
    public int TotalCount { get; } = schedules.Count;
}
