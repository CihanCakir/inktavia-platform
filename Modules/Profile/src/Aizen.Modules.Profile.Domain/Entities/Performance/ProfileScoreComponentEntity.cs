using Aizen.Core.Domain;
using Aizen.Modules.Profile.Abstraction.Enums.Performance;

namespace Aizen.Modules.Profile.Domain.Entities.Performance;

/// <summary>
/// Per-dimension score breakdown for the current snapshot.
/// One row per (SnapshotId, Category). Deleted and re-inserted on each
/// score recalculation to always reflect the latest computation.
///
/// MetricsJson stores the individual metric key/value pairs used to compute
/// RawScore (e.g. {"sr_completion_rate":0.92,"sr_response_rate":0.88}).
/// </summary>
[DocumentationInfo("ProfileScoreComponentEntity",
    "Per-dimension score breakdown linked to a ProfilePerformanceSnapshot. " +
    "Replaced on every recalculation. MetricsJson holds individual metric key/values.")]
public sealed class ProfileScoreComponentEntity : AizenEntityWithAudit
{
    // ── Identity ──────────────────────────────────────────────────────────────
    public long                      SnapshotId  { get; private set; }
    /// <summary>Denormalized for direct queries without joining snapshots.</summary>
    public long                      ProfileId   { get; private set; }
    public ProfileType               ProfileType { get; private set; }

    // ── Dimension ─────────────────────────────────────────────────────────────
    public PerformanceScoreCategory  Category    { get; private set; }

    // ── Scores ────────────────────────────────────────────────────────────────
    /// <summary>0–100 raw score for this dimension before weighting.</summary>
    public decimal RawScore            { get; private set; }
    /// <summary>Weight factor applied (e.g. 0.35 for ServiceRequest).</summary>
    public decimal Weight              { get; private set; }
    /// <summary>RawScore * Weight — contribution to the final OverallScore.</summary>
    public decimal WeightedContribution { get; private set; }

    // ── Metadata ──────────────────────────────────────────────────────────────
    /// <summary>Number of individual metrics aggregated into RawScore.</summary>
    public int     MetricCount  { get; private set; }
    /// <summary>JSON map of metric key → value used in computation.</summary>
    public string? MetricsJson  { get; private set; }

    public DateTime CalculatedAtUtc { get; private set; }

    private ProfileScoreComponentEntity() { }

    public static ProfileScoreComponentEntity Create(
        long                    snapshotId,
        long                    profileId,
        ProfileType             profileType,
        PerformanceScoreCategory category,
        decimal                 rawScore,
        decimal                 weight,
        int                     metricCount,
        string?                 metricsJson = null)
    {
        var raw        = Math.Clamp(rawScore, 0m, 100m);
        var wt         = Math.Clamp(weight, 0m, 1m);

        return new ProfileScoreComponentEntity
        {
            SnapshotId           = snapshotId,
            ProfileId            = profileId,
            ProfileType          = profileType,
            Category             = category,
            RawScore             = raw,
            Weight               = wt,
            WeightedContribution = Math.Round(raw * wt, 4),
            MetricCount          = Math.Max(0, metricCount),
            MetricsJson          = metricsJson,
            CalculatedAtUtc      = DateTime.UtcNow,
            IsActive             = true,
        };
    }
}
