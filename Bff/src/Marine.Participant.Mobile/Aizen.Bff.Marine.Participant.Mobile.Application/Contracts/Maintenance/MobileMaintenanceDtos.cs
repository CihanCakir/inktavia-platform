namespace Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Maintenance;

// BE_MO8 — owner maintenance self-service. The owner views/creates/edits/activates recurring maintenance schedules
// for their OWN vessels. Cost-free & owner-scoped: the OwnerUserId is stamped from the token (never the body) and the
// N2 reminder marker (ReminderSentAt) is internal — both are dropped from the owner payload.

/// <summary>One of the owner's maintenance schedules (cost-free — no OwnerUserId echo, no N2 ReminderSentAt marker).</summary>
public sealed class MobileMaintenanceScheduleDto
{
    public long Id { get; set; }
    public long VesselId { get; set; }
    public string ServiceCategoryCode { get; set; } = default!;
    /// <summary>Null = the whole category (no specific service type).</summary>
    public string? ServiceTypeCode { get; set; }
    public int RecommendedIntervalMonths { get; set; }
    public int ReminderLeadDays { get; set; }
    /// <summary>UTC; null = never performed.</summary>
    public DateTime? LastPerformedAt { get; set; }
    /// <summary>UTC due date = (LastPerformedAt ?? created) + interval. The FE highlights overdue / due-soon.</summary>
    public DateTime NextDueAt { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
}

/// <summary>The owner's own schedules + a count.</summary>
public sealed class MobileMaintenanceScheduleListDto
{
    public List<MobileMaintenanceScheduleDto> Schedules { get; set; } = new();
    public int TotalCount => Schedules.Count;
}

/// <summary>Create/edit payload — NO OwnerUserId (stamped from the token) and NO IsActive (use set-active).
/// Idempotent by (VesselId, ServiceCategoryCode, ServiceTypeCode). Vessel-ownership is BFF-gated.</summary>
public sealed class MobileUpsertMaintenanceScheduleRequest
{
    public long VesselId { get; set; }
    public string ServiceCategoryCode { get; set; } = default!;
    public string? ServiceTypeCode { get; set; }
    /// <summary>Months between services (≥1); null → the server default.</summary>
    public int? RecommendedIntervalMonths { get; set; }
    /// <summary>Days before NextDueAt to remind (≥0); null → the server default.</summary>
    public int? ReminderLeadDays { get; set; }
    /// <summary>Optional backfill of the last-performed date (re-anchors NextDueAt).</summary>
    public DateTime? LastPerformedAt { get; set; }
    public string? Notes { get; set; }
}

/// <summary>The upsert result — the schedule id, whether it was created (vs an existing active one updated), and the
/// recomputed next-due date.</summary>
public sealed class MobileMaintenanceUpsertResultDto
{
    public long ScheduleId { get; set; }
    public bool Created { get; set; }
    public DateTime NextDueAt { get; set; }
}

/// <summary>Activate/deactivate payload.</summary>
public sealed class MobileSetMaintenanceScheduleActiveRequest
{
    public bool IsActive { get; set; }
}

/// <summary>The set-active result.</summary>
public sealed class MobileMaintenanceSetActiveResultDto
{
    public long ScheduleId { get; set; }
    public bool IsActive { get; set; }
}
