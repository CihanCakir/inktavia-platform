using Aizen.Core.Domain;
using Aizen.Modules.Profile.Abstraction.Enums.Performance;

namespace Aizen.Modules.Profile.Domain.Entities.Performance;

/// <summary>
/// Short-lived cache for individual computed metric values used in the
/// ProfilePerformanceEngine. Avoids repeated cross-schema queries on each
/// score recalculation.
///
/// One row per (ProfileId, ProfileType, MetricKey).
/// Records can be refreshed by the engine; expired records are skipped and recomputed.
///
/// Example MetricKey values:
///   "sr_completion_rate", "sr_response_rate_within_2h", "sr_dispute_rate",
///   "cd_sell_through_rate", "cd_activation_rate", "payout_failure_rate"
/// </summary>
[DocumentationInfo("ProfileMetricCacheEntity",
    "Short-lived cache for individual performance metrics per (ProfileId, ProfileType, MetricKey). " +
    "Refreshed by the engine during score recalculation. Expired entries are recomputed on next run.")]
public sealed class ProfileMetricCacheEntity : AizenEntityWithAudit
{
    // ── Identity ──────────────────────────────────────────────────────────────
    public long        ProfileId   { get; private set; }
    public ProfileType ProfileType { get; private set; }

    // ── Cache entry ───────────────────────────────────────────────────────────
    /// <summary>Machine-readable metric key (e.g. "sr_completion_rate").</summary>
    public string    MetricKey        { get; private set; } = default!;
    /// <summary>Serialised numeric or boolean value (e.g. "0.92" or "true").</summary>
    public string    MetricValueJson  { get; private set; } = default!;

    public DateTime  CachedAtUtc   { get; private set; }
    public DateTime? ExpiresAtUtc  { get; private set; }

    private ProfileMetricCacheEntity() { }

    public static ProfileMetricCacheEntity Create(
        long        profileId,
        ProfileType profileType,
        string      metricKey,
        string      metricValueJson,
        TimeSpan?   ttl = null)
    {
        var now = DateTime.UtcNow;
        return new ProfileMetricCacheEntity
        {
            ProfileId       = profileId,
            ProfileType     = profileType,
            MetricKey       = metricKey,
            MetricValueJson = metricValueJson,
            CachedAtUtc     = now,
            ExpiresAtUtc    = ttl.HasValue ? now.Add(ttl.Value) : null,
            IsActive        = true,
        };
    }

    /// <summary>Updates the cached value and extends the TTL.</summary>
    public void Refresh(string newMetricValueJson, TimeSpan? newTtl = null)
    {
        var now         = DateTime.UtcNow;
        MetricValueJson = newMetricValueJson;
        CachedAtUtc     = now;
        ExpiresAtUtc    = newTtl.HasValue ? now.Add(newTtl.Value) : ExpiresAtUtc;
    }

    public bool IsExpired() => ExpiresAtUtc.HasValue && ExpiresAtUtc.Value < DateTime.UtcNow;
}
