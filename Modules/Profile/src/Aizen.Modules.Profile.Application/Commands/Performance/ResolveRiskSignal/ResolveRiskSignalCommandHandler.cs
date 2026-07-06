using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Profile.Abstraction.Dtos.Performance;
using Aizen.Modules.Profile.Abstraction.Enums.Performance;
using Aizen.Modules.Profile.Domain.Entities.Performance;
using Aizen.Modules.Profile.Domain.Interface.Repository;

namespace Aizen.Modules.Profile.Application.Commands.Performance.ResolveRiskSignal;

[DocumentationInfo("ResolveRiskSignalCommandHandler",
    "Resolves an active risk signal by id. " +
    "After resolution, checks whether any remaining High/Critical signals exist. " +
    "If none remain, clears the Flagged tier override on the snapshot and recalculates tier from current score. " +
    "Appends a RiskSignalResolved decision log entry. " +
    "Phase 19 rule: tier restoration does NOT re-run the scoring engine — it uses the current snapshot score.")]
public sealed class ResolveRiskSignalCommandHandler
    : AizenCommandHandler<ResolveRiskSignalCommand, ProfileRiskSignalDto>
{
    private readonly IProfileRiskSignalRepository          _signals;
    private readonly IProfilePerformanceSnapshotRepository _snapshots;
    private readonly IProfileDecisionLogRepository         _decisionLogs;

    public ResolveRiskSignalCommandHandler(
        IProfileRiskSignalRepository          signals,
        IProfilePerformanceSnapshotRepository snapshots,
        IProfileDecisionLogRepository         decisionLogs)
    {
        _signals      = signals;
        _snapshots    = snapshots;
        _decisionLogs = decisionLogs;
    }

    public override async Task<ProfileRiskSignalDto> Handle(
        ResolveRiskSignalCommand request, CancellationToken ct)
    {
        var signal = await _signals.GetByIdAsync(request.SignalId, ct)
            ?? throw new InvalidOperationException($"Risk signal {request.SignalId} not found.");

        signal.Resolve(request.ResolutionNote, request.ResolvedByUserId);
        _signals.Update(signal);
        await _signals.SaveChangesAsync(ct);

        // Check whether any remaining High/Critical signals exist
        var hasRemainingHighRisk = await _signals.HasActiveSignalAboveAsync(
            signal.ProfileId, signal.ProfileType,
            RiskSignalSeverity.High, ct);

        var snapshot = await _snapshots.GetByProfileAsync(
            signal.ProfileId, signal.ProfileType, ct);

        if (snapshot is not null && !hasRemainingHighRisk)
        {
            // Restore tier from current score: re-derive without calling the engine
            var restoredTier = Derivetier(snapshot.OverallScore, snapshot.ConfidenceScore);
            var previousTier = snapshot.PriorityTier;

            snapshot.ClearRiskSignalFlag(restoredTier);
            _snapshots.Update(snapshot);
            await _snapshots.SaveChangesAsync(ct);

            var log = ProfileDecisionLogEntity.Create(
                profileId:        signal.ProfileId,
                profileType:      signal.ProfileType,
                eventType:        DecisionLogEventType.RiskSignalResolved,
                eventDescription: $"Risk signal resolved: [{signal.SignalCode}]. Tier restored from Flagged to {restoredTier}.",
                previousTier:     previousTier,
                newTier:          restoredTier,
                actorUserId:      request.ActorUserId);

            await _decisionLogs.AddAsync(log, ct);
            await _decisionLogs.SaveChangesAsync(ct);
        }

        return new ProfileRiskSignalDto
        {
            Id               = signal.Id,
            ProfileId        = signal.ProfileId,
            ProfileType      = signal.ProfileType,
            Severity         = signal.Severity,
            SignalCode       = signal.SignalCode,
            Description      = signal.Description,
            SourceModule     = signal.SourceModule,
            SourceEntityId   = signal.SourceEntityId,
            DetectedAtUtc    = signal.DetectedAtUtc,
            IsResolved       = signal.IsResolved,
            ResolvedAtUtc    = signal.ResolvedAtUtc,
            ResolutionNote   = signal.ResolutionNote,
            ResolvedByUserId = signal.ResolvedByUserId,
        };
    }

    /// <summary>
    /// Re-derives priority tier from score + confidence without re-running the engine.
    /// Must stay in sync with ProfilePerformanceEngine.ClassifyTier.
    /// </summary>
    private static PriorityTier Derivetier(decimal overallScore, decimal confidence) =>
        (overallScore, confidence) switch
        {
            ( >= 90m, >= 0.70m) => PriorityTier.Platinum,
            ( >= 75m, >= 0.50m) => PriorityTier.Gold,
            ( >= 60m, >= 0.40m) => PriorityTier.Silver,
            _                   => PriorityTier.Standard,
        };
}
