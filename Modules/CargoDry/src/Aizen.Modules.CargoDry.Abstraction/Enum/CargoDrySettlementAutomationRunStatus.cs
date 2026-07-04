namespace Aizen.Modules.CargoDry.Abstraction.Enum;

/// <summary>
/// Lifecycle status of a CargoDry monthly settlement automation run.
/// A run is a single execution of the automation service for a given target year-month.
/// Phase 6 (July 2026).
/// </summary>
public enum CargoDrySettlementAutomationRunStatus
{
    /// <summary>Run record created; processing has not yet started.</summary>
    Pending = 1,

    /// <summary>Processing is currently in progress.</summary>
    Running = 2,

    /// <summary>All eligible settlements were processed without errors.</summary>
    Completed = 3,

    /// <summary>Processing completed but one or more settlements could not be processed.</summary>
    PartiallyCompleted = 4,

    /// <summary>Run aborted due to an unexpected fatal error before all items were processed.</summary>
    Failed = 5,

    /// <summary>Run was a DryRun (preview-only). No mutations were performed.</summary>
    DryRunCompleted = 6,
}
