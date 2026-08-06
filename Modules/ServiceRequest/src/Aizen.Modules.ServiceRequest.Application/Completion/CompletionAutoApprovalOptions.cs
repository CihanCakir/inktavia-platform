using Microsoft.Extensions.Configuration;

namespace Aizen.Modules.ServiceRequest.Application.Completion;

/// <summary>
/// N3-C — the admin-tunable knobs for completion auto-approval, read from configuration (deploy-time overridable;
/// documented defaults, no hardcoded magic constants at the call sites). Centralised + pure so the window/lead-day
/// math is unit-testable and the submit handler and the job read the same keys.
///
/// <para>Keys (section <c>ServiceRequest</c>): <c>AutoApproveWindowDays</c> (default 7), <c>AutoApproveReminderLeadDays</c>
/// (default 2), <c>AutoApproveSystemActorUserId</c> (default 0), <c>AutoApproveBatchSize</c> (default 200). If runtime
/// tunability is later required, swap the reads for the ReferenceData SystemParameter service.</para>
/// </summary>
public static class CompletionAutoApprovalOptions
{
    public const int DefaultWindowDays       = 7;
    public const int DefaultReminderLeadDays = 2;
    public const int DefaultBatchSize        = 200;

    public static int WindowDays(IConfiguration config)
        => Sanitize(config.GetValue("ServiceRequest:AutoApproveWindowDays", DefaultWindowDays), DefaultWindowDays, min: 1);

    public static int ReminderLeadDays(IConfiguration config)
    {
        var window = WindowDays(config);
        var lead   = Sanitize(config.GetValue("ServiceRequest:AutoApproveReminderLeadDays", DefaultReminderLeadDays),
            DefaultReminderLeadDays, min: 0);
        // The reminder must land strictly inside the window (a lead ≥ window would fire immediately at submit).
        return Math.Min(lead, Math.Max(window - 1, 0));
    }

    public static long SystemActorUserId(IConfiguration config)
        => config.GetValue<long>("ServiceRequest:AutoApproveSystemActorUserId", 0);

    public static int BatchSize(IConfiguration config)
        => Sanitize(config.GetValue("ServiceRequest:AutoApproveBatchSize", DefaultBatchSize), DefaultBatchSize, min: 1);

    /// <summary>The frozen-at-submission deadline.</summary>
    public static DateTime ComputeAutoApproveAt(DateTime submittedAtUtc, int windowDays)
        => submittedAtUtc.AddDays(windowDays);

    private static int Sanitize(int value, int fallback, int min) => value >= min ? value : fallback;
}
