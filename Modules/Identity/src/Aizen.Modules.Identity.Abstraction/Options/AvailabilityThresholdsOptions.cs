namespace Aizen.Modules.Identity.Abstraction.Options;

/// <summary>
/// Config-driven thresholds for the coarse provider-area availability bucketing (M2). Bound from
/// <c>Availability:Thresholds</c>. The verdict is derived from these values — NOT compiled-in <c>if</c> constants —
/// so the coarse bands can be retuned without a deploy. Defaults: <c>none</c>=0 eligible, <c>limited</c>=1..2,
/// <c>available</c>=3+.
/// </summary>
public sealed class AvailabilityThresholdsOptions
{
    public const string SectionName = "Availability:Thresholds";

    /// <summary>Minimum eligible providers to be at least "limited" (default 1). Below this ⇒ "none".</summary>
    public int LimitedMin { get; set; } = 1;

    /// <summary>Minimum eligible providers to be "available" (default 3). Also the row cap fetched by the query.</summary>
    public int AvailableMin { get; set; } = 3;
}
