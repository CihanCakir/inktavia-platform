using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Profile.Abstraction.Enums.Performance;
using Aizen.Modules.Profile.Application.Queries.Performance.GetPerformanceSnapshotByProfile;
using Aizen.Modules.Profile.Abstraction.Interface.Service;
using Aizen.Modules.Profile.Domain.Entities.Performance;
using Aizen.Modules.Profile.Domain.Interface.Repository;

namespace Aizen.Modules.Profile.Application.Commands.Performance.UpsertPerformanceSnapshot;

[DocumentationInfo("UpsertPerformanceSnapshotCommandHandler",
    "Full score recalculation pipeline for Phase 19. Steps: " +
    "(1) Run ProfilePerformanceEngine.CalculateAsync, " +
    "(2) Upsert snapshot (Create or UpdateScore), " +
    "(3) Check active risk signals → apply Flagged tier if High/Critical, " +
    "(4) Replace score components, " +
    "(5) Append score history entry, " +
    "(6) Append decision log entry (ScoreRecalculated / TierChanged / ColdStartBaseline). " +
    "Phase 19 rule: no automatic punishment, no commission changes, no payout holds.")]
public sealed class UpsertPerformanceSnapshotCommandHandler
    : AizenCommandHandler<UpsertPerformanceSnapshotCommand, UpsertPerformanceSnapshotResult>
{
    private readonly IProfilePerformanceEngine              _engine;
    private readonly IProfilePerformanceSnapshotRepository _snapshots;
    private readonly IProfileScoreComponentRepository      _components;
    private readonly IProfileScoreHistoryRepository        _history;
    private readonly IProfileDecisionLogRepository         _decisionLogs;
    private readonly IProfileRiskSignalRepository          _riskSignals;

    public UpsertPerformanceSnapshotCommandHandler(
        IProfilePerformanceEngine              engine,
        IProfilePerformanceSnapshotRepository  snapshots,
        IProfileScoreComponentRepository       components,
        IProfileScoreHistoryRepository         history,
        IProfileDecisionLogRepository          decisionLogs,
        IProfileRiskSignalRepository           riskSignals)
    {
        _engine       = engine;
        _snapshots    = snapshots;
        _components   = components;
        _history      = history;
        _decisionLogs = decisionLogs;
        _riskSignals  = riskSignals;
    }

    public override async Task<UpsertPerformanceSnapshotResult> Handle(
        UpsertPerformanceSnapshotCommand request, CancellationToken ct)
    {
        // ── 1. Run the scoring engine ─────────────────────────────────────────
        var calc = await _engine.CalculateAsync(request.ProfileId, request.ProfileType, ct);

        // ── 2. Load or create the snapshot ───────────────────────────────────
        var existing = await _snapshots.GetByProfileAsync(
            request.ProfileId, request.ProfileType, ct);

        var previousTier = existing?.PriorityTier ?? PriorityTier.Standard;

        ProfilePerformanceSnapshotEntity snapshot;
        bool isNew;

        if (existing is null)
        {
            snapshot = ProfilePerformanceSnapshotEntity.Create(
                profileId:                  calc.ProfileId,
                profileType:                calc.ProfileType,
                overallScore:               calc.OverallScore,
                serviceRequestScore:        calc.ServiceRequestScore,
                cargoDryScore:              calc.CargoDryScore,
                operationalDisciplineScore: calc.OperationalDisciplineScore,
                financialReliabilityScore:  calc.FinancialReliabilityScore,
                platformComplianceScore:    calc.PlatformComplianceScore,
                riskPenaltyScore:           calc.RiskPenaltyScore,
                confidenceScore:            calc.ConfidenceScore,
                confidenceLevel:            calc.ConfidenceLevel,
                sampleSize:                 calc.SampleSize,
                priorityTier:               calc.DerivedTier,
                metadataJson:               calc.MetadataJson);

            await _snapshots.AddAsync(snapshot, ct);
            isNew = true;
        }
        else
        {
            existing.UpdateScore(
                overallScore:               calc.OverallScore,
                serviceRequestScore:        calc.ServiceRequestScore,
                cargoDryScore:              calc.CargoDryScore,
                operationalDisciplineScore: calc.OperationalDisciplineScore,
                financialReliabilityScore:  calc.FinancialReliabilityScore,
                platformComplianceScore:    calc.PlatformComplianceScore,
                riskPenaltyScore:           calc.RiskPenaltyScore,
                confidenceScore:            calc.ConfidenceScore,
                confidenceLevel:            calc.ConfidenceLevel,
                sampleSize:                 calc.SampleSize,
                priorityTier:               calc.DerivedTier,
                metadataJson:               calc.MetadataJson);

            _snapshots.Update(existing);
            snapshot = existing;
            isNew    = false;
        }

        // ── 3. Apply Flagged tier override if active High/Critical risk signal ──
        var maxSeverity = await _riskSignals.GetMaxActiveSeverityAsync(
            request.ProfileId, request.ProfileType, ct);

        if (maxSeverity is RiskSignalSeverity.High or RiskSignalSeverity.Critical)
        {
            snapshot.FlagRiskSignal(maxSeverity.Value);
        }
        else if (!isNew && snapshot.HasActiveRiskSignal)
        {
            // Active signals all cleared — restore calculated tier
            snapshot.ClearRiskSignalFlag(calc.DerivedTier);
        }

        await _snapshots.SaveChangesAsync(ct);

        // ── 4. Replace score components ───────────────────────────────────────
        var componentEntities = calc.Components.Select(c =>
            ProfileScoreComponentEntity.Create(
                snapshotId:  snapshot.Id,
                profileId:   request.ProfileId,
                profileType: request.ProfileType,
                category:    c.Category,
                rawScore:    c.RawScore,
                weight:      c.Weight,
                metricCount: c.MetricCount,
                metricsJson: c.MetricsJson))
            .ToList();

        await _components.ReplaceForSnapshotAsync(snapshot.Id, componentEntities, ct);

        // ── 5. Append score history ───────────────────────────────────────────
        var historyEntry = ProfileScoreHistoryEntity.Create(
            profileId:      request.ProfileId,
            profileType:    request.ProfileType,
            overallScore:   snapshot.OverallScore,
            priorityTier:   snapshot.PriorityTier,
            confidenceScore: snapshot.ConfidenceScore,
            sampleSize:     snapshot.SampleSize,
            triggerReason:  request.TriggerReason,
            metadataJson:   calc.MetadataJson);

        await _history.AddAsync(historyEntry, ct);
        await _history.SaveChangesAsync(ct);

        // ── 6. Append decision log ─────────────────────────────────────────────
        var newTier    = snapshot.PriorityTier;
        var tierChanged = !isNew && newTier != previousTier;

        var eventType = calc.IsColdStart
            ? DecisionLogEventType.ColdStartApplied
            : tierChanged
                ? DecisionLogEventType.TierChanged
                : DecisionLogEventType.ScoreCalculated;

        var description = calc.IsColdStart
            ? $"Cold-start baseline assigned. SampleSize={calc.SampleSize}."
            : tierChanged
                ? $"Tier changed from {previousTier} to {newTier}. Score: {calc.OverallScore:F1}."
                : $"Score recalculated. Overall={calc.OverallScore:F1}, Tier={newTier}.";

        var logEntry = ProfileDecisionLogEntity.Create(
            profileId:        request.ProfileId,
            profileType:      request.ProfileType,
            eventType:        eventType,
            eventDescription: description,
            previousTier:     isNew ? null : previousTier,
            newTier:          newTier,
            previousScore:    isNew ? null : (decimal?)null,
            newScore:         snapshot.OverallScore,
            actorUserId:      request.ActorUserId,
            metadataJson:     calc.MetadataJson);

        await _decisionLogs.AddAsync(logEntry, ct);
        await _decisionLogs.SaveChangesAsync(ct);

        // ── 7. Return result ──────────────────────────────────────────────────
        return new UpsertPerformanceSnapshotResult
        {
            Snapshot     = GetPerformanceSnapshotByProfileQueryHandler.MapSnapshot(snapshot),
            PreviousTier = previousTier,
            NewTier      = newTier,
            TierChanged  = tierChanged,
            IsColdStart  = calc.IsColdStart,
        };
    }
}
