namespace Aizen.Modules.Profile.Abstraction.Enums.Performance;

/// <summary>
/// Classifies entries in the append-only ProfileDecisionLog.
/// </summary>
public enum DecisionLogEventType
{
    /// <summary>Score was recalculated (periodic or on-demand).</summary>
    ScoreCalculated    = 1,

    /// <summary>PriorityTier changed from one value to another.</summary>
    TierChanged        = 2,

    /// <summary>A new risk signal was detected and recorded.</summary>
    RiskSignalRaised   = 3,

    /// <summary>An active risk signal was resolved.</summary>
    RiskSignalResolved = 4,

    /// <summary>Score or tier was overridden manually by an admin.</summary>
    ManualOverride     = 5,

    /// <summary>Cold-start baseline was applied (SampleSize &lt; 5).</summary>
    ColdStartApplied   = 6,
}
