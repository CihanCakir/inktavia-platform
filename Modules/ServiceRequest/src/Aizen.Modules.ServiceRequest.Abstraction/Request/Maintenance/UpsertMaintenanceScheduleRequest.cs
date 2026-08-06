namespace Aizen.Modules.ServiceRequest.Abstraction.Request.Maintenance;

/// <summary>
/// S12 — admin create/upsert of a recurring maintenance schedule (owner self-service later). Interval + lead are
/// optional; when omitted the configurable <c>MaintenanceScheduleOptions</c> defaults apply (no hardcoded constants).
/// Upsert key = (VesselId, ServiceCategoryCode, ServiceTypeCode) among active schedules.
/// </summary>
[DocumentationInfo("Upsert maintenance schedule request", "Create or update a per-vessel, per-category maintenance schedule.")]
public sealed class UpsertMaintenanceScheduleRequest
{
    public long VesselId { get; set; }
    public long OwnerUserId { get; set; }
    public string ServiceCategoryCode { get; set; } = default!;
    public string? ServiceTypeCode { get; set; }

    /// <summary>Months between maintenances (≥ 1). Null → config default.</summary>
    public int? RecommendedIntervalMonths { get; set; }

    /// <summary>Days before NextDueAt to remind (≥ 0). Null → config default.</summary>
    public int? ReminderLeadDays { get; set; }

    /// <summary>Optional backfill of the last performed date (advances the cycle immediately). UTC.</summary>
    public DateTime? LastPerformedAt { get; set; }

    public string? Notes { get; set; }
}
