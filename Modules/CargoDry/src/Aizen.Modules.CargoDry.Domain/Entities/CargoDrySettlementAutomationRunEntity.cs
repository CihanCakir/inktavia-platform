using Aizen.Core.Domain;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Domain.Entities;

/// <summary>
/// Records a single execution of the CargoDry monthly settlement automation service.
/// Each run targets one calendar month (TargetYearMonth) and may process multiple settlements.
/// A run is always triggered manually by an admin. The recurring job (when enabled) also
/// triggers runs, but they are persisted identically to manual runs.
///
/// Hard invariants (Phase 6, July 2026):
///   - AutoCompletePayout is ALWAYS false. This field exists only for audit transparency.
///   - Mode = DryRun runs produce DryRunCompleted status and no settlement mutations.
///   - Mode = Live runs dispatch existing CQRS command handlers; never bypass them.
///   - The run NEVER marks a settlement as Settled.
///   - The run NEVER calls Approve/Processing/Complete/Fail payout commands.
/// </summary>
[DocumentationInfo("CargoDry Settlement Automation Run",
    "Records a single execution of the monthly settlement automation service for a given target year-month. " +
    "Contains aggregate counters and links to per-settlement run items. " +
    "AutoCompletePayout is always false — payout lifecycle is always manual. " +
    "Phase 6 (July 2026).")]
public sealed class CargoDrySettlementAutomationRunEntity : AizenEntityWithAudit
{
    // ── Identity ────────────────────────────────────────────────────────────────
    /// <summary>Human-readable unique run code, e.g. "SAR-202606-0001".</summary>
    public string RunCode { get; private set; } = default!;

    // ── Run configuration ────────────────────────────────────────────────────────
    /// <summary>
    /// Year + month the automation targeted, formatted as YYYYMM (e.g. 202606 for June 2026).
    /// Settlements with PeriodStartUtc.Year*100 + PeriodStartUtc.Month == TargetYearMonth are in scope.
    /// </summary>
    public int TargetYearMonth { get; private set; }

    /// <summary>DryRun (preview only) or Live (mutations applied).</summary>
    public CargoDrySettlementAutomationMode Mode { get; private set; }

    /// <summary>Current lifecycle status of this run.</summary>
    public CargoDrySettlementAutomationRunStatus Status { get; private set; }

    // ── Safety flags (audit transparency) ────────────────────────────────────────
    /// <summary>
    /// ALWAYS false. Payout completion is a manual admin operation.
    /// Stored for audit transparency; no code path ever sets this to true.
    /// </summary>
    public bool AutoCompletePayout { get; private set; } = false;

    /// <summary>
    /// When true (and Mode = Live), the run will call PrepareCargoDrySettlementPayment for each
    /// newly ReadyForSettlement settlement after resolving it. Default: false.
    /// </summary>
    public bool AutoPreparePayment { get; private set; }

    /// <summary>
    /// When true (and Mode = Live), the run will call PrepareCargoDrySettlementInvoice for each
    /// newly ReadyForSettlement settlement after payment preparation. Default: false.
    /// Only effective if AutoPreparePayment is also true.
    /// </summary>
    public bool AutoPrepareInvoice { get; private set; }

    // ── Trigger ──────────────────────────────────────────────────────────────────
    /// <summary>Admin user who triggered this run. Required; runs are always manually triggered.</summary>
    public long TriggeredByUserId { get; private set; }

    /// <summary>UTC timestamp when the run was created and processing began.</summary>
    public DateTime TriggeredAtUtc { get; private set; }

    /// <summary>UTC timestamp when the run reached a terminal status (Completed/Failed/DryRunCompleted).</summary>
    public DateTime? CompletedAtUtc { get; private set; }

    /// <summary>Total wall-clock duration of the run in milliseconds.</summary>
    public long? DurationMs { get; private set; }

    // ── Aggregate counters ────────────────────────────────────────────────────────
    /// <summary>Total settlements found for TargetYearMonth regardless of status.</summary>
    public int TotalSettlementsFound { get; private set; }

    /// <summary>Settlements that were in an eligible status for automation processing.</summary>
    public int TotalSettlementsEligible { get; private set; }

    /// <summary>Settlements successfully processed (at least one action applied).</summary>
    public int TotalSettlementsProcessed { get; private set; }

    /// <summary>Settlements skipped because they were not in a processable state (e.g., already ReadyForSettlement with no auto-payment).</summary>
    public int TotalSettlementsSkipped { get; private set; }

    /// <summary>Settlements where an error occurred during processing.</summary>
    public int TotalSettlementsErrored { get; private set; }

    // ── Notes ────────────────────────────────────────────────────────────────────
    /// <summary>Optional admin note attached at run creation time.</summary>
    public string? Note { get; private set; }

    /// <summary>Aggregate error summary if any settlements errored. Set at run completion.</summary>
    public string? ErrorSummary { get; private set; }

    // ── Timestamps ───────────────────────────────────────────────────────────────
    public DateTime CreatedAtUtc { get; private set; }

    // ── Navigation ───────────────────────────────────────────────────────────────
    private readonly List<CargoDrySettlementAutomationRunItemEntity> _runItems = new();
    public IReadOnlyList<CargoDrySettlementAutomationRunItemEntity> RunItems => _runItems.AsReadOnly();

    private CargoDrySettlementAutomationRunEntity() { }

    // ── Factory ──────────────────────────────────────────────────────────────────
    public static CargoDrySettlementAutomationRunEntity Create(
        string                             runCode,
        int                                targetYearMonth,
        CargoDrySettlementAutomationMode   mode,
        bool                               autoPreparePayment,
        bool                               autoPrepareInvoice,
        long                               triggeredByUserId,
        string?                            note,
        DateTime                           nowUtc)
    {
        if (targetYearMonth < 200001 || targetYearMonth > 209912)
            throw new ArgumentOutOfRangeException(nameof(targetYearMonth),
                "TargetYearMonth must be a valid YYYYMM integer between 200001 and 209912.");

        if (string.IsNullOrWhiteSpace(runCode))
            throw new ArgumentException("RunCode must not be empty.", nameof(runCode));

        return new CargoDrySettlementAutomationRunEntity
        {
            RunCode              = runCode,
            TargetYearMonth      = targetYearMonth,
            Mode                 = mode,
            Status               = CargoDrySettlementAutomationRunStatus.Pending,
            AutoCompletePayout   = false,  // HARD INVARIANT: always false
            AutoPreparePayment   = autoPreparePayment,
            AutoPrepareInvoice   = autoPrepareInvoice,
            TriggeredByUserId    = triggeredByUserId,
            TriggeredAtUtc       = nowUtc,
            Note                 = note,
            CreatedAtUtc         = nowUtc,
        };
    }

    // ── Domain methods ────────────────────────────────────────────────────────────

    public void MarkRunning(DateTime nowUtc)
    {
        if (Status != CargoDrySettlementAutomationRunStatus.Pending)
            throw new InvalidOperationException($"Cannot mark run as Running from status {Status}.");
        Status = CargoDrySettlementAutomationRunStatus.Running;
    }

    public void Complete(
        int      totalFound,
        int      totalEligible,
        int      totalProcessed,
        int      totalSkipped,
        int      totalErrored,
        string?  errorSummary,
        DateTime nowUtc)
    {
        TotalSettlementsFound     = totalFound;
        TotalSettlementsEligible  = totalEligible;
        TotalSettlementsProcessed = totalProcessed;
        TotalSettlementsSkipped   = totalSkipped;
        TotalSettlementsErrored   = totalErrored;
        ErrorSummary              = errorSummary;
        CompletedAtUtc            = nowUtc;
        DurationMs                = (long)(nowUtc - TriggeredAtUtc).TotalMilliseconds;

        Status = Mode == CargoDrySettlementAutomationMode.DryRun
            ? CargoDrySettlementAutomationRunStatus.DryRunCompleted
            : (totalErrored > 0 && totalProcessed == 0)
                ? CargoDrySettlementAutomationRunStatus.Failed
                : (totalErrored > 0)
                    ? CargoDrySettlementAutomationRunStatus.PartiallyCompleted
                    : CargoDrySettlementAutomationRunStatus.Completed;
    }

    public void Fail(string errorSummary, DateTime nowUtc)
    {
        Status       = CargoDrySettlementAutomationRunStatus.Failed;
        ErrorSummary = errorSummary;
        CompletedAtUtc = nowUtc;
        DurationMs   = (long)(nowUtc - TriggeredAtUtc).TotalMilliseconds;
    }

    public void AddItem(CargoDrySettlementAutomationRunItemEntity item)
        => _runItems.Add(item);
}
