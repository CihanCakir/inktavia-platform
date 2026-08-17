using Aizen.Core.Domain;
using Aizen.Modules.Profile.Abstraction.Enums.Performance;

namespace Aizen.Modules.Profile.Domain.Entities.Performance;

/// <summary>
/// Append-only audit log for all performance decisions affecting a profile.
/// Captures score recalculations, tier changes, risk signal events, and manual overrides.
///
/// INVARIANT: Records must never be mutated or deleted after creation.
/// The repository exposes only AddAsync — no Update or Delete methods.
/// ActorUserId = null means the event was triggered by the system (scheduled job / engine).
/// </summary>
[DocumentationInfo("ProfileDecisionLogEntity",
    "Append-only audit log for all performance decisions. " +
    "Covers score calculations, tier changes, risk signals, manual overrides, and cold-start baseline.")]
public sealed class ProfileDecisionLogEntity : AizenEntityWithAudit
{
    // ── Identity ──────────────────────────────────────────────────────────────
    public long               ProfileId   { get; private set; }
    public ProfileType        ProfileType { get; private set; }

    // ── Event ─────────────────────────────────────────────────────────────────
    public DecisionLogEventType EventType        { get; private set; }
    public string               EventDescription { get; private set; } = default!;

    // ── Before / After ────────────────────────────────────────────────────────
    public PriorityTier? PreviousTier  { get; private set; }
    public PriorityTier? NewTier       { get; private set; }
    public decimal?      PreviousScore { get; private set; }
    public decimal?      NewScore      { get; private set; }

    // ── Actor ─────────────────────────────────────────────────────────────────
    /// <summary>Keycloak sub or internal user ID. Null = system-triggered.</summary>
    public string? ActorUserId { get; private set; }

    // ── Timestamp ─────────────────────────────────────────────────────────────
    public DateTime OccurredAtUtc { get; private set; }
    public string?  MetadataJson  { get; private set; }

    private ProfileDecisionLogEntity() { }

    public static ProfileDecisionLogEntity Create(
        long                profileId,
        ProfileType         profileType,
        DecisionLogEventType eventType,
        string              eventDescription,
        PriorityTier?       previousTier  = null,
        PriorityTier?       newTier       = null,
        decimal?            previousScore = null,
        decimal?            newScore      = null,
        string?             actorUserId   = null,
        string?             metadataJson  = null)
    {
        return new ProfileDecisionLogEntity
        {
            ProfileId        = profileId,
            ProfileType      = profileType,
            EventType        = eventType,
            EventDescription = eventDescription,
            PreviousTier     = previousTier,
            NewTier          = newTier,
            PreviousScore    = previousScore,
            NewScore         = newScore,
            ActorUserId      = actorUserId,
            OccurredAtUtc    = DateTime.UtcNow,
            MetadataJson     = metadataJson,
            IsActive         = true,
        };
    }
}
