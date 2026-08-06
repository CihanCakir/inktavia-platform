using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.ServiceRequest.Application.Command.Maintenance;

/// <summary>
/// S12 — internal command (dispatched by the completion-approved consumer): advance the active maintenance schedule
/// that matches a completed service request's (vessel, category, type). No public contract.
/// </summary>
[DocumentationInfo("Advance maintenance schedule command", "Advances the matching schedule when a service is completed (S12).")]
public sealed class AdvanceMaintenanceScheduleCommand : AizenCommand<AdvanceMaintenanceScheduleResponse>
{
    public long ServiceRequestId { get; }
    public AdvanceMaintenanceScheduleCommand(long serviceRequestId) => ServiceRequestId = serviceRequestId;
}

/// <summary>Internal result — whether a schedule was advanced (and its id).</summary>
public sealed class AdvanceMaintenanceScheduleResponse
{
    public bool Advanced { get; init; }
    public long? ScheduleId { get; init; }
}
