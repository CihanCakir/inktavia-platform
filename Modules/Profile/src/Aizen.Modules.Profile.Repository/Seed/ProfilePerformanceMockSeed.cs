using Aizen.Modules.Profile.Abstraction.Enums.Performance;
using Aizen.Modules.Profile.Domain.Entities.Performance;
using Aizen.Modules.Profile.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Profile.Repository.Seed;

/// <summary>
/// Seeds three representative ProfilePerformanceSnapshot rows for admin panel
/// testing and demonstration purposes.
///
/// Coverage:
///   SNAP-001  Provider 11011 → Cold-start Standard (SampleSize=2, Score=50, coldStart:true)
///   SNAP-002  Provider 11012 → Gold tier (SampleSize=18, Score=78.5, conf=0.90)
///   SNAP-003  Provider 11013 → Flagged tier (SampleSize=12, Score=72.0 but risk signal active)
///
/// Phase 19 rule: seed never triggers scoring engine — data is inserted directly.
/// Idempotent — skipped if any snapshot already exists.
/// </summary>
public sealed class ProfilePerformanceMockSeed
{
    private readonly ProfileDbContext                  _db;
    private readonly ILogger<ProfilePerformanceMockSeed> _logger;

    public ProfilePerformanceMockSeed(
        ProfileDbContext                  db,
        ILogger<ProfilePerformanceMockSeed> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await _db.PerformanceSnapshots.AnyAsync(ct))
        {
            _logger.LogDebug("ProfilePerformanceMockSeed skipped — data already present.");
            return;
        }

        var now = DateTime.UtcNow;

        // ── SNAP-001: Provider 11011 — Cold-start Standard ──────────────────
        // SampleSize=2 → cold-start rule applies: score=50, confidence=0.10, tier=Standard.
        var snap1 = ProfilePerformanceSnapshotEntity.Create(
            profileId:                  11011L,
            profileType:                ProfileType.Provider,
            overallScore:               50m,
            serviceRequestScore:        50m,
            cargoDryScore:              50m,
            operationalDisciplineScore: 50m,
            financialReliabilityScore:  50m,
            platformComplianceScore:    50m,
            riskPenaltyScore:           0m,
            confidenceScore:            0.10m,
            confidenceLevel:            PerformanceConfidenceLevel.ColdStart,
            sampleSize:                 2,
            priorityTier:               PriorityTier.Standard,
            metadataJson:               """{"coldStart":true,"calculationVersion":"v1","triggerReason":"InitialSeed"}""");

        await _db.PerformanceSnapshots.AddAsync(snap1, ct);
        await _db.SaveChangesAsync(ct);

        AddComponents(_db, snap1.Id, 11011L, ProfileType.Provider,
            sr: 50m, cd: 50m, od: 50m, fr: 50m, pc: 50m,
            srMetrics: """{"sr_completion_rate":null,"sr_ontime_rate":null}""",
            cdMetrics: """{"cd_activation_rate":null}""",
            odMetrics: """{"od_response_rate":null}""",
            frMetrics: """{"fr_payout_failure_rate":null}""",
            pcMetrics: """{"pc_profile_completeness":null}""");

        await _db.SaveChangesAsync(ct);

        // ── SNAP-002: Provider 11012 — Gold tier ────────────────────────────
        // SampleSize=18 → confidence = 18/20 = 0.90. Score > 75 with conf >= 0.50 → Gold.
        // OverallScore = 85*0.35 + 80*0.25 + 78*0.15 + 72*0.15 + 68*0.10 = 79.45
        var snap2 = ProfilePerformanceSnapshotEntity.Create(
            profileId:                  11012L,
            profileType:                ProfileType.Provider,
            overallScore:               79.45m,
            serviceRequestScore:        85m,
            cargoDryScore:              80m,
            operationalDisciplineScore: 78m,
            financialReliabilityScore:  72m,
            platformComplianceScore:    68m,
            riskPenaltyScore:           0m,
            confidenceScore:            0.90m,
            confidenceLevel:            PerformanceConfidenceLevel.High,
            sampleSize:                 18,
            priorityTier:               PriorityTier.Gold,
            metadataJson:               """{"coldStart":false,"calculationVersion":"v1","triggerReason":"InitialSeed"}""");

        await _db.PerformanceSnapshots.AddAsync(snap2, ct);
        await _db.SaveChangesAsync(ct);

        AddComponents(_db, snap2.Id, 11012L, ProfileType.Provider,
            sr: 85m, cd: 80m, od: 78m, fr: 72m, pc: 68m,
            srMetrics: """{"sr_completion_rate":0.92,"sr_ontime_rate":0.88,"sr_rebook_rate":0.21,"sr_dispute_rate":0.04,"sr_avg_client_rating":4.4,"sr_response_rate":0.91}""",
            cdMetrics: """{"cd_activation_rate":0.85,"cd_renewal_rate":0.76,"cd_kit_expiry_rate":0.08}""",
            odMetrics: """{"od_response_rate":0.90,"od_phase_completion_rate":0.82,"od_document_submission_rate":0.95}""",
            frMetrics: """{"fr_payout_failure_rate":0.02,"fr_tx_dispute_rate":0.03,"fr_invoice_overdue_rate":0.00}""",
            pcMetrics: """{"pc_profile_completeness":0.88,"pc_terms_acceptance":1.00,"pc_kyc_status":1.00}""");

        await _db.SaveChangesAsync(ct);

        // ── SNAP-003: Provider 11013 — Flagged ──────────────────────────────
        // SampleSize=12, score=72 normally → Silver (72 >= 60, conf=0.60 >= 0.40).
        // But a HIGH risk signal is active → tier overridden to Flagged.
        var snap3 = ProfilePerformanceSnapshotEntity.Create(
            profileId:                  11013L,
            profileType:                ProfileType.Provider,
            overallScore:               72.0m,
            serviceRequestScore:        68m,
            cargoDryScore:              75m,
            operationalDisciplineScore: 70m,
            financialReliabilityScore:  55m,       // low: repeated payout failures
            platformComplianceScore:    82m,
            riskPenaltyScore:           0m,
            confidenceScore:            0.60m,
            confidenceLevel:            PerformanceConfidenceLevel.Medium,
            sampleSize:                 12,
            priorityTier:               PriorityTier.Silver,   // will be overridden below
            metadataJson:               """{"coldStart":false,"calculationVersion":"v1","triggerReason":"InitialSeed"}""");

        await _db.PerformanceSnapshots.AddAsync(snap3, ct);
        await _db.SaveChangesAsync(ct);

        // Apply Flagged override: active HIGH risk signal (repeated payout failures)
        snap3.FlagRiskSignal(RiskSignalSeverity.High);
        _db.PerformanceSnapshots.Update(snap3);

        AddComponents(_db, snap3.Id, 11013L, ProfileType.Provider,
            sr: 68m, cd: 75m, od: 70m, fr: 55m, pc: 82m,
            srMetrics: """{"sr_completion_rate":0.80,"sr_ontime_rate":0.75,"sr_rebook_rate":0.15,"sr_dispute_rate":0.09,"sr_avg_client_rating":3.9,"sr_response_rate":0.84}""",
            cdMetrics: """{"cd_activation_rate":0.78,"cd_renewal_rate":0.72,"cd_kit_expiry_rate":0.14}""",
            odMetrics: """{"od_response_rate":0.82,"od_phase_completion_rate":0.76,"od_document_submission_rate":0.88}""",
            frMetrics: """{"fr_payout_failure_rate":0.18,"fr_tx_dispute_rate":0.12,"fr_invoice_overdue_rate":0.06}""",
            pcMetrics: """{"pc_profile_completeness":0.95,"pc_terms_acceptance":1.00,"pc_kyc_status":1.00}""");

        // Seed the risk signal that triggered the Flagged override
        var signal = ProfileRiskSignalEntity.Create(
            profileId:     11013L,
            profileType:   ProfileType.Provider,
            severity:      RiskSignalSeverity.High,
            signalCode:    "HIGH_PAYOUT_FAILURE_RATE",
            description:   "Provider payout failure rate exceeded 15% threshold over the last 30 days.",
            sourceModule:  "Payment",
            sourceEntityId: null);

        await _db.RiskSignals.AddAsync(signal, ct);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "ProfilePerformanceMockSeed completed. Inserted 3 snapshots, 15 components, 1 risk signal.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void AddComponents(
        ProfileDbContext db,
        long             snapshotId,
        long             profileId,
        ProfileType      profileType,
        decimal sr, decimal cd, decimal od, decimal fr, decimal pc,
        string  srMetrics, string  cdMetrics, string  odMetrics, string  frMetrics, string  pcMetrics)
    {
        db.ScoreComponents.AddRange(
            ProfileScoreComponentEntity.Create(snapshotId, profileId, profileType,
                PerformanceScoreCategory.ServiceRequest,        sr, 0.35m, 6, srMetrics),
            ProfileScoreComponentEntity.Create(snapshotId, profileId, profileType,
                PerformanceScoreCategory.CargoDry,              cd, 0.25m, 3, cdMetrics),
            ProfileScoreComponentEntity.Create(snapshotId, profileId, profileType,
                PerformanceScoreCategory.OperationalDiscipline, od, 0.15m, 3, odMetrics),
            ProfileScoreComponentEntity.Create(snapshotId, profileId, profileType,
                PerformanceScoreCategory.FinancialReliability,  fr, 0.15m, 3, frMetrics),
            ProfileScoreComponentEntity.Create(snapshotId, profileId, profileType,
                PerformanceScoreCategory.PlatformCompliance,    pc, 0.10m, 3, pcMetrics));
    }
}
