namespace Aizen.Modules.ServiceRequest.Abstraction.Request.Maintenance;

/// <summary>
/// S12 — admin toggle of a maintenance schedule's active state. Deactivating frees the active-unique
/// (vessel, category, type) slot and stops N2 reminders; reactivating re-arms it (unless another active
/// schedule already owns the same key, which fails with a clean business error).
/// </summary>
[DocumentationInfo("Set maintenance schedule active request", "Activate or deactivate a maintenance schedule (S12).")]
public sealed class SetMaintenanceScheduleActiveRequest
{
    public bool IsActive { get; set; }
}
