using Microsoft.Extensions.Configuration;

namespace Aizen.Modules.ServiceRequest.Application.Maintenance;

/// <summary>
/// S12/N2 — admin-tunable defaults for the recurring maintenance schedule + its due-reminder, read from configuration
/// (deploy-time overridable; documented defaults, no hardcoded magic constants at the call sites). Every value is
/// per-schedule on the entity; these are only the fall-backs when a create request omits them, plus the reminder-job
/// batch size. Centralised + pure so the create command and the reminder job read the same keys.
///
/// <para>Keys (section <c>ServiceRequest</c>): <c>MaintenanceDefaultIntervalMonths</c> (default 12),
/// <c>MaintenanceDefaultReminderLeadDays</c> (default 14), <c>MaintenanceReminderBatchSize</c> (default 200),
/// <c>MaintenanceAutoCreateOnCompletion</c> (default false — MVP: only advance existing schedules). If runtime
/// tunability is later required, swap the reads for the ReferenceData SystemParameter service.</para>
/// </summary>
public static class MaintenanceScheduleOptions
{
    public const int DefaultIntervalMonths   = 12;
    public const int DefaultReminderLeadDays = 14;
    public const int DefaultBatchSize        = 200;

    public static int DefaultInterval(IConfiguration config)
        => Sanitize(config.GetValue("ServiceRequest:MaintenanceDefaultIntervalMonths", DefaultIntervalMonths),
            DefaultIntervalMonths, min: 1);

    public static int DefaultLeadDays(IConfiguration config)
        => Sanitize(config.GetValue("ServiceRequest:MaintenanceDefaultReminderLeadDays", DefaultReminderLeadDays),
            DefaultReminderLeadDays, min: 0);

    public static int ReminderBatchSize(IConfiguration config)
        => Sanitize(config.GetValue("ServiceRequest:MaintenanceReminderBatchSize", DefaultBatchSize),
            DefaultBatchSize, min: 1);

    /// <summary>Config flag (default OFF): auto-create a schedule on a completion whose category has none. MVP = off.</summary>
    public static bool AutoCreateOnCompletion(IConfiguration config)
        => config.GetValue("ServiceRequest:MaintenanceAutoCreateOnCompletion", false);

    private static int Sanitize(int value, int fallback, int min) => value >= min ? value : fallback;
}
