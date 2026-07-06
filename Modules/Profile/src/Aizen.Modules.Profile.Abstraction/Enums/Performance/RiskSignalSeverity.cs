namespace Aizen.Modules.Profile.Abstraction.Enums.Performance;

/// <summary>
/// Severity level of an active performance risk signal.
/// Critical and High signals trigger the Flagged PriorityTier.
/// </summary>
public enum RiskSignalSeverity
{
    Low      = 1,
    Medium   = 2,
    High     = 3,
    Critical = 4,
}
