namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Maintenance;

[DocumentationInfo("Upsert maintenance schedule response", "Result of creating or updating a maintenance schedule.")]
public sealed class UpsertMaintenanceScheduleResponse(long scheduleId, bool created, DateTime nextDueAt)
{
    public long ScheduleId { get; } = scheduleId;
    /// <summary>True when a new schedule was created; false when an existing active one was updated (idempotent upsert).</summary>
    public bool Created { get; } = created;
    public DateTime NextDueAt { get; } = nextDueAt;
}
