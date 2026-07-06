using Aizen.Core.Domain;
using Aizen.Modules.Profile.Abstraction.Enums.Performance;

namespace Aizen.Modules.Profile.Domain.Entities.Performance;

/// <summary>
/// Append-only score history log. A new entry is written every time the
/// ProfilePerformanceEngine recalculates a profile's score.
///
/// INVARIANT: Records must never be mutated or deleted after creation.
/// The repository exposes only AddAsync — no Update or Delete methods.
/// </summary>
[DocumentationInfo("ProfileScoreHistoryEntity",
    "Append-only score history. One entry per recalculation. " +
    "Used for trend charts, audit, and tier change analytics.")]
public sealed class ProfileScoreHistoryEntity : AizenEntityWithAudit
{
    // ── Identity ──────────────────────────────────────────────────────────────
    public long        ProfileId   { get; private set; }
    public ProfileType ProfileType { get; private set; }

    // ── Snapshot at time of recording ─────────────────────────────────────────
    public decimal     OverallScore    { get; private set; }
    public PriorityTier PriorityTier   { get; private set; }
    public decimal     ConfidenceScore { get; private set; }
    public int         SampleSize      { get; private set; }

    // ── Provenance ────────────────────────────────────────────────────────────
    public DateTime RecordedAtUtc { get; private set; }
    /// <summary>Human-readable reason (e.g. "ScheduledRecalculation", "RiskSignalRaised").</summary>
    public string?  TriggerReason { get; private set; }
    public string?  MetadataJson  { get; private set; }

    private ProfileScoreHistoryEntity() { }

    public static ProfileScoreHistoryEntity Create(
        long        profileId,
        ProfileType profileType,
        decimal     overallScore,
        PriorityTier priorityTier,
        decimal     confidenceScore,
        int         sampleSize,
        string?     triggerReason = null,
        string?     metadataJson  = null)
    {
        return new ProfileScoreHistoryEntity
        {
            ProfileId       = profileId,
            ProfileType     = profileType,
            OverallScore    = Math.Clamp(overallScore, 0m, 100m),
            PriorityTier    = priorityTier,
            ConfidenceScore = Math.Clamp(confidenceScore, 0m, 1m),
            SampleSize      = Math.Max(0, sampleSize),
            RecordedAtUtc   = DateTime.UtcNow,
            TriggerReason   = triggerReason,
            MetadataJson    = metadataJson,
            IsActive        = true,
        };
    }
}
