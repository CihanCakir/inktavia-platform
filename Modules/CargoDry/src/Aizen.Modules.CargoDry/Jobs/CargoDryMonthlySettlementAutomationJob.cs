using Aizen.Core.Scheduler;
using Aizen.Core.Scheduler.Abstraction;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Abstraction.Interface.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Application.Jobs;

/// <summary>
/// Scheduled monthly settlement automation job.
/// DISABLED by default (IsActive = false). Must be manually enabled via configuration override.
/// CRON: "0 2 1 * *" = 02:00 UTC on the 1st of every month.
///
/// When enabled and triggered, runs the automation in DryRun mode by default.
/// To run in Live mode, the job must be intentionally reconfigured — this prevents
/// accidental mutations from a misconfigured scheduler.
///
/// Hard constraints enforced server-side:
///   - AutoCompletePayout is always false (invariant on CargoDrySettlementAutomationRunEntity)
///   - AutoPreparePayment = false (job default)
///   - AutoPrepareInvoice = false (job default)
///   - Mode = DryRun (job default)
///
/// Phase 6 (July 2026).
/// </summary>
public sealed class CargoDryMonthlySettlementAutomationJob : AizenRecurringJob
{
    public CargoDryMonthlySettlementAutomationJob(
        IAizenSchedulerLogger logger,
        IServiceProvider      serviceProvider)
        : base(logger, serviceProvider) { }

    /// <summary>
    /// Disabled by default. Set to true only after explicit business sign-off.
    /// </summary>
    public override bool   IsActive       => false;

    /// <summary>
    /// 02:00 UTC on the 1st of every month.
    /// </summary>
    public override string CronExpression => "0 2 1 * *";

    protected override async Task ProcessAsync(CancellationToken cancellationToken)
    {
        Logger.WriteConsole("CargoDryMonthlySettlementAutomationJob: starting");

        using var scope   = ServiceProvider.CreateScope();
        var automation    = scope.ServiceProvider
            .GetRequiredService<ICargoDryMonthlySettlementAutomationService>();

        // Target the previous month (job runs on the 1st, so previous month's settlements are due)
        var now           = DateTime.UtcNow;
        var targetMonth   = now.Month == 1
            ? new DateTime(now.Year - 1, 12, 1)
            : new DateTime(now.Year, now.Month - 1, 1);
        var targetYearMonth = targetMonth.Year * 100 + targetMonth.Month;

        // RunCode for scheduler-triggered runs uses a dedicated prefix.
        // Sequence = 0 signals a scheduler-originated run; full sequence tracking
        // is handled by the command handler when dispatched via API (ManualTrigger).
        // For the job we generate a deterministic code: SAR-{YYYYMM}-9000 (reserved range).
        var runCode = $"SAR-{targetYearMonth}-9000";

        Logger.WriteConsole(
            $"CargoDryMonthlySettlementAutomationJob: target={targetYearMonth}, runCode={runCode}, mode=DryRun");

        try
        {
            var run = await automation.RunAsync(
                runCode:            runCode,
                targetYearMonth:    targetYearMonth,
                mode:               CargoDrySettlementAutomationMode.DryRun, // default: never mutate from job
                autoPreparePayment: false,
                autoPrepareInvoice: false,
                triggeredByUserId:  0L, // 0 = system/scheduler
                note:               "Triggered by CargoDryMonthlySettlementAutomationJob (DryRun).",
                cancellationToken);

            Logger.WriteConsole(
                $"CargoDryMonthlySettlementAutomationJob: completed. " +
                $"Status={run.Status}, " +
                $"Found={run.TotalSettlementsFound}, " +
                $"Eligible={run.TotalSettlementsEligible}, " +
                $"Processed={run.TotalSettlementsProcessed}, " +
                $"Errored={run.TotalSettlementsErrored}");
        }
        catch (Exception ex)
        {
            Logger.WriteConsole(
                $"CargoDryMonthlySettlementAutomationJob: unhandled exception — {ex.Message}");
            throw;
        }
    }
}
