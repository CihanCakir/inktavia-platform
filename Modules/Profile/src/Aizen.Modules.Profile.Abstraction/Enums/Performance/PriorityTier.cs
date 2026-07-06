namespace Aizen.Modules.Profile.Abstraction.Enums.Performance;

/// <summary>
/// Priority tier assigned to a profile based on OverallScore + ConfidenceScore.
/// Thresholds (Phase 19 spec):
///   Platinum : OverallScore ≥ 90 AND ConfidenceScore ≥ 0.70
///   Gold     : OverallScore ≥ 75 AND ConfidenceScore ≥ 0.50
///   Silver   : OverallScore ≥ 60 AND ConfidenceScore ≥ 0.40
///   Standard : default / cold-start / below Silver
///   Flagged  : active high-severity risk signal present (overrides all other tiers)
/// </summary>
public enum PriorityTier
{
    Standard = 0,
    Silver   = 1,
    Gold     = 2,
    Platinum = 3,

    /// <summary>
    /// Active high-severity risk signal present.
    /// Overrides every other tier classification regardless of score.
    /// </summary>
    Flagged  = 99,
}
