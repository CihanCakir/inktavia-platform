using Aizen.Core.Domain;
using Aizen.Modules.Profile.Abstraction.Enums.Performance;

namespace Aizen.Modules.Profile.Domain.Entities.Performance;

/// <summary>
/// Active and resolved risk signals for a profile.
/// High and Critical severity signals override PriorityTier to Flagged.
///
/// Signals are raised by the performance engine when specific thresholds are breached
/// (e.g. dispute rate > 30%, 3+ payout failures). They must be resolved manually
/// or by an automated rule — the engine never auto-resolves signals.
/// </summary>
[DocumentationInfo("ProfileRiskSignalEntity",
    "Risk signals raised by the performance engine or admin. " +
    "High/Critical signals override PriorityTier to Flagged. " +
    "Resolution is manual or rule-based, never automatic.")]
public sealed class ProfileRiskSignalEntity : AizenEntityWithAudit
{
    // ── Identity ──────────────────────────────────────────────────────────────
    public long               ProfileId   { get; private set; }
    public ProfileType        ProfileType { get; private set; }

    // ── Signal ────────────────────────────────────────────────────────────────
    public RiskSignalSeverity Severity    { get; private set; }
    /// <summary>Machine-readable code (e.g. "HIGH_DISPUTE_RATE", "PAYOUT_FAILURE_REPEAT").</summary>
    public string             SignalCode  { get; private set; } = default!;
    public string             Description { get; private set; } = default!;

    // ── Source ────────────────────────────────────────────────────────────────
    /// <summary>Module that produced the signal ("ServiceRequest", "CargoDry", "Payment").</summary>
    public string? SourceModule   { get; private set; }
    /// <summary>ID of the entity (e.g. DisputeId, PayoutRecordId) that triggered the signal.</summary>
    public long?   SourceEntityId { get; private set; }

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    public DateTime  DetectedAtUtc    { get; private set; }
    public bool      IsResolved       { get; private set; }
    public DateTime? ResolvedAtUtc    { get; private set; }
    public string?   ResolutionNote   { get; private set; }
    public long?     ResolvedByUserId { get; private set; }

    private ProfileRiskSignalEntity() { }

    public static ProfileRiskSignalEntity Create(
        long               profileId,
        ProfileType        profileType,
        RiskSignalSeverity severity,
        string             signalCode,
        string             description,
        string?            sourceModule   = null,
        long?              sourceEntityId = null)
    {
        return new ProfileRiskSignalEntity
        {
            ProfileId      = profileId,
            ProfileType    = profileType,
            Severity       = severity,
            SignalCode     = signalCode,
            Description    = description,
            SourceModule   = sourceModule,
            SourceEntityId = sourceEntityId,
            DetectedAtUtc  = DateTime.UtcNow,
            IsResolved     = false,
            IsActive       = true,
        };
    }

    /// <summary>Marks the signal as resolved. Only call once — throws if already resolved.</summary>
    public void Resolve(string? resolutionNote, long? resolvedByUserId)
    {
        if (IsResolved)
            throw new InvalidOperationException($"Risk signal {Id} is already resolved.");

        IsResolved       = true;
        ResolvedAtUtc    = DateTime.UtcNow;
        ResolutionNote   = resolutionNote;
        ResolvedByUserId = resolvedByUserId;
        IsActive         = false;
    }
}
