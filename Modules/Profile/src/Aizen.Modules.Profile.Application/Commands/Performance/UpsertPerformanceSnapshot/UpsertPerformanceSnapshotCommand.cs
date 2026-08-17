using Aizen.Core.CQRS.Message;
using Aizen.Modules.Profile.Abstraction.Dtos.Performance;
using Aizen.Modules.Profile.Abstraction.Enums.Performance;

namespace Aizen.Modules.Profile.Application.Commands.Performance.UpsertPerformanceSnapshot;

/// <summary>
/// Triggers a full score recalculation for the given profile and persists the result:
///   1. Runs ProfilePerformanceEngine.CalculateAsync
///   2. Upserts the snapshot (Create or UpdateScore)
///   3. Applies Flagged tier override if active High/Critical risk signal exists
///   4. Replaces score components
///   5. Appends a score history entry
///   6. Appends a decision log entry (ScoreRecalculated, TierChanged, ColdStartBaseline)
/// </summary>
public sealed class UpsertPerformanceSnapshotCommand
    : AizenCommand<UpsertPerformanceSnapshotResult>
{
    public long        ProfileId     { get; init; }
    public ProfileType ProfileType   { get; init; }
    /// <summary>Human-readable reason persisted in history entry (e.g. "ScheduledRecalculation", "AdminForced").</summary>
    public string?     TriggerReason { get; init; }
    /// <summary>Keycloak sub of the actor (null = system-triggered).</summary>
    public string?     ActorUserId   { get; init; }
}

public sealed record UpsertPerformanceSnapshotResult
{
    public ProfilePerformanceSnapshotDto Snapshot   { get; init; } = default!;
    public PriorityTier                  PreviousTier { get; init; }
    public PriorityTier                  NewTier      { get; init; }
    public bool                          TierChanged  { get; init; }
    public bool                          IsColdStart  { get; init; }
}
