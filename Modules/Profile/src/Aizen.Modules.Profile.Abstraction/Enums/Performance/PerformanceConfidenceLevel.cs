namespace Aizen.Modules.Profile.Abstraction.Enums.Performance;

/// <summary>
/// Human-readable confidence band derived from ConfidenceScore.
/// ConfidenceScore = min(1.0, SampleSize / 20.0)
///
/// ColdStart : SampleSize &lt; 5  → score = 50 (neutral), ConfidenceScore ≤ 0.25
/// Low       : 0.25 &lt;  ConfidenceScore ≤ 0.50
/// Medium    : 0.50 &lt;  ConfidenceScore ≤ 0.80
/// High      : ConfidenceScore &gt; 0.80
/// </summary>
public enum PerformanceConfidenceLevel
{
    ColdStart = 0,
    Low       = 1,
    Medium    = 2,
    High      = 3,
}
