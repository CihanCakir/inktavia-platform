using Aizen.Core.Domain;

namespace Aizen.Modules.ServiceRequest.Domain.Entities.Maintenance;

/// <summary>
/// S12 (§20.14) — a per-vessel, per-category recurring maintenance schedule. Configurable interval + reminder lead
/// (per-schedule; defaults from <c>MaintenanceScheduleOptions</c>). The daily N2 job scans it and reminds the owner
/// before <see cref="NextDueAt"/>; a matching completion advances it via <see cref="RecordPerformed"/>.
///
/// <para><b>Separate from CargoDry renewal</b> — this is the SR maintenance cadence, not the CargoDry kit lifecycle.
/// All timestamps are UTC (timestamptz). Unique per active (VesselId, ServiceCategoryCode[, ServiceTypeCode]).</para>
/// </summary>
[DocumentationInfo("Maintenance schedule entity", "Per-vessel, per-category recurring maintenance schedule (S12).")]
public sealed class MaintenanceScheduleEntity : AizenEntityWithAudit
{
    public long VesselId { get; private set; }
    public string ServiceCategoryCode { get; private set; } = default!;
    /// <summary>Optional finer key. Null = the schedule covers the whole category.</summary>
    public string? ServiceTypeCode { get; private set; }
    public long OwnerUserId { get; private set; }

    /// <summary>Configurable cadence in months (≥ 1). No hardcoded constant — defaulted by MaintenanceScheduleOptions.</summary>
    public int RecommendedIntervalMonths { get; private set; }

    /// <summary>UTC of the last performed maintenance (from a matching completion, or an explicit backfill). Null = never.</summary>
    public DateTime? LastPerformedAt { get; private set; }

    /// <summary>UTC when the next maintenance is due = (LastPerformedAt ?? created-anchor) + RecommendedIntervalMonths.</summary>
    public DateTime NextDueAt { get; private set; }

    /// <summary>Configurable days before <see cref="NextDueAt"/> the reminder fires (≥ 0).</summary>
    public int ReminderLeadDays { get; private set; }

    /// <summary>UTC the current-cycle reminder was sent — the once-guard. Reset to null when the cycle advances.</summary>
    public DateTime? ReminderSentAt { get; private set; }

    public string? Notes { get; private set; }

    public MaintenanceScheduleEntity() { }

    /// <summary>
    /// Validating factory. NextDueAt = (lastPerformedAt ?? now) + interval (the "created-anchor" is the creation moment
    /// when the schedule was never performed). Reminder is un-armed (ReminderSentAt = null).
    /// </summary>
    public static MaintenanceScheduleEntity Create(
        long vesselId,
        string serviceCategoryCode,
        string? serviceTypeCode,
        long ownerUserId,
        int recommendedIntervalMonths,
        int reminderLeadDays,
        DateTime? lastPerformedAt = null,
        string? notes = null)
    {
        if (vesselId <= 0)
            throw new ArgumentOutOfRangeException(nameof(vesselId), vesselId, "VesselId must be positive.");
        if (string.IsNullOrWhiteSpace(serviceCategoryCode))
            throw new ArgumentException("ServiceCategoryCode is required.", nameof(serviceCategoryCode));
        if (ownerUserId <= 0)
            throw new ArgumentOutOfRangeException(nameof(ownerUserId), ownerUserId, "OwnerUserId must be positive.");
        if (recommendedIntervalMonths < 1)
            throw new ArgumentOutOfRangeException(nameof(recommendedIntervalMonths), recommendedIntervalMonths,
                "RecommendedIntervalMonths must be ≥ 1.");
        if (reminderLeadDays < 0)
            throw new ArgumentOutOfRangeException(nameof(reminderLeadDays), reminderLeadDays,
                "ReminderLeadDays must be ≥ 0.");

        var anchor = (lastPerformedAt ?? DateTime.UtcNow).ToUniversalTime();

        return new MaintenanceScheduleEntity
        {
            VesselId                  = vesselId,
            ServiceCategoryCode       = serviceCategoryCode.Trim(),
            ServiceTypeCode           = string.IsNullOrWhiteSpace(serviceTypeCode) ? null : serviceTypeCode.Trim(),
            OwnerUserId               = ownerUserId,
            RecommendedIntervalMonths = recommendedIntervalMonths,
            ReminderLeadDays          = reminderLeadDays,
            LastPerformedAt           = lastPerformedAt?.ToUniversalTime(),
            NextDueAt                 = anchor.AddMonths(recommendedIntervalMonths),
            ReminderSentAt            = null,
            Notes                     = notes,
            IsActive                  = true,
        };
    }

    /// <summary>
    /// S12 — a matching maintenance was performed: stamp LastPerformedAt, recompute NextDueAt for the next cycle, and
    /// <b>re-arm the reminder</b> (ReminderSentAt = null) so N2 can remind again before the new due date.
    /// </summary>
    public void RecordPerformed(DateTime performedAtUtc)
    {
        var performed = performedAtUtc.ToUniversalTime();
        LastPerformedAt = performed;
        NextDueAt       = performed.AddMonths(RecommendedIntervalMonths);
        ReminderSentAt  = null;
    }

    /// <summary>
    /// Admin re-configure (upsert-update): change interval/lead/notes and recompute NextDueAt from the same anchor
    /// (LastPerformedAt if performed, else the row's creation date). The shifted due date re-arms the reminder.
    /// </summary>
    public void UpdateSchedule(int recommendedIntervalMonths, int reminderLeadDays, string? notes)
    {
        if (recommendedIntervalMonths < 1)
            throw new ArgumentOutOfRangeException(nameof(recommendedIntervalMonths), recommendedIntervalMonths,
                "RecommendedIntervalMonths must be ≥ 1.");
        if (reminderLeadDays < 0)
            throw new ArgumentOutOfRangeException(nameof(reminderLeadDays), reminderLeadDays,
                "ReminderLeadDays must be ≥ 0.");

        RecommendedIntervalMonths = recommendedIntervalMonths;
        ReminderLeadDays          = reminderLeadDays;
        Notes                     = notes;

        var anchor = (LastPerformedAt ?? CreateDate ?? DateTime.UtcNow).ToUniversalTime();
        NextDueAt      = anchor.AddMonths(recommendedIntervalMonths);
        ReminderSentAt = null;
    }

    /// <summary>N2 — true once the reminder window has opened (now ≥ NextDueAt − ReminderLeadDays).</summary>
    public bool IsInReminderWindow(DateTime nowUtc) => nowUtc >= NextDueAt.AddDays(-ReminderLeadDays);

    /// <summary>N2 — the schedule wants a reminder now: active, un-armed this cycle, and inside the window.</summary>
    public bool NeedsReminder(DateTime nowUtc)
        => IsActive && ReminderSentAt is null && IsInReminderWindow(nowUtc);

    /// <summary>N2 — stamp that the current-cycle reminder was published (once-guard, multi-replica safe).</summary>
    public void MarkReminderSent(DateTime nowUtc) => ReminderSentAt = nowUtc.ToUniversalTime();

    /// <summary>Soft-deactivate (keeps history; frees the active-unique slot).</summary>
    public void Deactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;
}
