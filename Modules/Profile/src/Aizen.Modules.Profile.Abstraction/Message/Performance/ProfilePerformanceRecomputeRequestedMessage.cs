using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.Profile.Abstraction.Message.Performance;

/// <summary>
/// Internal message that requests a full performance score recalculation for a profile.
///
/// Published by: ProfilePerformanceSignalConsumer (in response to external domain events)
/// Consumed by: ProfilePerformanceRecomputeRequestedConsumer
/// Exchange/queue: profile.performance.recompute-requested
///
/// ── Reliability guarantees ─────────────────────────────────────────────────────
///   - Published only from ExecuteCommitMessage (after Prepare phase succeeded) — no fire-and-forget.
///   - Idempotency key prevents double-recalculation if the message is delivered more than once.
///   - TriggerReason is append-only and preserved in the resulting ProfileScoreHistoryEntity.
///
/// ── Provider-only rule ─────────────────────────────────────────────────────────
///   Phase 19 rule: only Provider profiles are scored. Consumer must guard against
///   non-Provider ProfileType and drop the message silently (return false in Prepare).
///
/// ── Audit trail ────────────────────────────────────────────────────────────────
///   SourceModule + SourceEntityId are forwarded to the command handler's TriggerReason
///   and stored in ProfileDecisionLogEntity.MetadataJson for full traceability.
/// </summary>
public sealed class ProfilePerformanceRecomputeRequestedMessage : AizenBaseMessage
{
    /// <summary>Internal profile ID (Provider/Owner/Participant long PK).</summary>
    public long ProfileId { get; init; }

    /// <summary>
    /// Profile type. Phase 19: only Provider = 1 is scored.
    /// Consumer must drop messages for non-Provider types.
    /// </summary>
    public int ProfileType { get; init; }

    /// <summary>
    /// Human-readable reason for the recalculation request.
    /// Stored in ProfileScoreHistoryEntity.TriggerReason and ProfileDecisionLogEntity.MetadataJson.
    /// </summary>
    public string TriggerReason { get; init; } = "EventTriggered";

    /// <summary>
    /// Name of the source module that triggered this recalculation (e.g. "ServiceRequest", "CargoDry", "Payment").
    /// Informational only — stored in metadata for audit trail.
    /// </summary>
    public string SourceModule { get; init; } = default!;

    /// <summary>
    /// ID of the source domain entity that triggered this recalculation (e.g. ServiceRequestId, KitId).
    /// Informational only — stored in metadata for audit trail.
    /// </summary>
    public long? SourceEntityId { get; init; }

    /// <summary>
    /// Deterministic idempotency key to prevent duplicate recalculations.
    /// Format: "{SourceModule}-{SourceEntityId ?? ProfileId}-{ProfileId}"
    /// Consumer drops the message if a ProfileScoreHistoryEntity already exists for this key within the same UTC day.
    /// </summary>
    public string IdempotencyKey { get; init; } = default!;

    /// <summary>UTC timestamp when this recompute was originally requested.</summary>
    public DateTime RequestedAtUtc { get; init; }
}
