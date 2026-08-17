using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Profile.Abstraction.Dtos.Performance;
using Aizen.Modules.Profile.Abstraction.Enums.Performance;
using Aizen.Modules.Profile.Domain.Entities.Performance;
using Aizen.Modules.Profile.Domain.Interface.Repository;

namespace Aizen.Modules.Profile.Application.Commands.Performance.RaiseRiskSignal;

[DocumentationInfo("RaiseRiskSignalCommandHandler",
    "Creates a new risk signal for a profile. " +
    "If severity is High or Critical, immediately sets snapshot.PriorityTier = Flagged " +
    "and appends a TierChanged decision log entry. " +
    "Phase 19 rule: no automatic commission changes, payout holds, or blocking.")]
public sealed class RaiseRiskSignalCommandHandler
    : AizenCommandHandler<RaiseRiskSignalCommand, ProfileRiskSignalDto>
{
    private readonly IProfileRiskSignalRepository          _signals;
    private readonly IProfilePerformanceSnapshotRepository _snapshots;
    private readonly IProfileDecisionLogRepository         _decisionLogs;

    public RaiseRiskSignalCommandHandler(
        IProfileRiskSignalRepository          signals,
        IProfilePerformanceSnapshotRepository snapshots,
        IProfileDecisionLogRepository         decisionLogs)
    {
        _signals      = signals;
        _snapshots    = snapshots;
        _decisionLogs = decisionLogs;
    }

    public override async Task<ProfileRiskSignalDto> Handle(
        RaiseRiskSignalCommand request, CancellationToken ct)
    {
        // Create the risk signal
        var signal = ProfileRiskSignalEntity.Create(
            profileId:     request.ProfileId,
            profileType:   request.ProfileType,
            severity:      request.Severity,
            signalCode:    request.SignalCode,
            description:   request.Description,
            sourceModule:  request.SourceModule,
            sourceEntityId: request.SourceEntityId);

        await _signals.AddAsync(signal, ct);
        await _signals.SaveChangesAsync(ct);

        // If High or Critical: flag the snapshot tier
        if (request.Severity is RiskSignalSeverity.High or RiskSignalSeverity.Critical)
        {
            var snapshot = await _snapshots.GetByProfileAsync(
                request.ProfileId, request.ProfileType, ct);

            if (snapshot is not null)
            {
                var previousTier = snapshot.PriorityTier;
                snapshot.FlagRiskSignal(request.Severity);
                _snapshots.Update(snapshot);
                await _snapshots.SaveChangesAsync(ct);

                var log = ProfileDecisionLogEntity.Create(
                    profileId:        request.ProfileId,
                    profileType:      request.ProfileType,
                    eventType:        DecisionLogEventType.RiskSignalRaised,
                    eventDescription: $"Risk signal raised: [{request.SignalCode}] {request.Description}. Tier overridden to Flagged.",
                    previousTier:     previousTier,
                    newTier:          PriorityTier.Flagged,
                    actorUserId:      request.ActorUserId);

                await _decisionLogs.AddAsync(log, ct);
                await _decisionLogs.SaveChangesAsync(ct);
            }
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
}
