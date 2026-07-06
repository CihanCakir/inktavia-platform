using System.Data;
using System.Data.Common;
using System.Text.Json;
using Aizen.Modules.Profile.Abstraction.Dtos.Performance;
using Aizen.Modules.Profile.Abstraction.Enums.Performance;
using Aizen.Modules.Profile.Abstraction.Interface.Service;
using Aizen.Modules.Profile.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Profile.Application.Performance;

/// <summary>
/// Calculates performance scores for Provider, Participant, and Owner profiles.
///
/// Cross-schema reads are performed via the ProfileDbContext ADO.NET connection —
/// same PostgreSQL instance, no HTTP calls, no module project dependencies.
///
/// Phase 19 rules (providers) + Phase 22 rules (participants) enforced here:
/// - No automatic punishment, blocking, commission changes, or payout holds.
/// - No scoring in BFF — BFF is proxy-only.
/// - Cold-start: Provider SampleSize &lt; 5 → neutral; Participant SampleSize &lt; 3 → neutral.
/// - Score history / decision logs are written by the calling CQRS handler, not here.
/// - Risk signal flag override (Flagged tier) is applied by the calling handler.
/// - Participant/Owner profiles use a different score formula (SR + CD + FR + PC; OD is neutral).
/// - Participant performance is admin visibility only — no enforcement, ranking, or restriction.
/// </summary>
[DocumentationInfo("ProfilePerformanceEngine",
    "Calculates performance score for Provider/Participant/Owner profiles. " +
    "Reads from servicerequest, cargodry, payment, and profile schemas via raw SQL. " +
    "Returns ProfileScoreCalculationResult — never mutates entities directly. " +
    "Phase 22: Added participant/owner branching. Participant score uses " +
    "SR*0.35 + CD*0.20 + FR*0.15 + PC*0.20 (OD neutral/excluded). " +
    "Participant identity resolved via UserProfiles.UserId lookup.")]
public sealed class ProfilePerformanceEngine : IProfilePerformanceEngine
{
    private readonly ProfileDbContext _db;

    // ── Provider formula constants ───────────────────────────────────────────
    private const int     ColdStartThreshold            = 5;
    private const int     ParticipantColdStartThreshold = 3;
    private const int     FullSampleSize                = 20;
    private const string  CalculationVersion            = "v1";
    private const decimal NeutralScore                  = 50m;

    // ── Status constants (int values stored in DB) ───────────────────────────
    private const int SrStatusCompleted          = 41;   // ServiceRequestStatus.Completed
    private const int SrStatusCancelled          = 90;   // ServiceRequestStatus.Cancelled
    private const int SrStatusDisputeOpened      = 50;   // ServiceRequestStatus.DisputeOpened
    private const int SrStatusUnderDisputeReview = 51;   // ServiceRequestStatus.UnderDisputeReview
    private const int SrStatusDisputeResolved    = 52;   // ServiceRequestStatus.DisputeResolved
    private const int SrDisputeStatusResolved    = 6;    // ServiceRequestDisputeStatus.Resolved
    private const int PayoutStatusFailed         = 4;    // PayoutStatus.Failed
    private const int TxStatusDisputed           = 7;    // PaymentTransactionStatus.Disputed
    private const int KitStatusActivated         = 2;    // CargoDryKitStatus.Activated
    private const int KitStatusExpired           = 3;    // CargoDryKitStatus.Expired
    private const int KitStatusRenewed           = 4;    // CargoDryKitStatus.Renewed
    private const int InvoiceStatusOverdue       = 6;    // InvoiceStatus.Overdue

    public ProfilePerformanceEngine(ProfileDbContext db)
    {
        _db = db;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public entry point — branches by ProfileType
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<ProfileScoreCalculationResult> CalculateAsync(
        long            profileId,
        ProfileType     profileType,
        CancellationToken ct = default)
    {
        var conn = _db.Database.GetDbConnection();
        if (conn.State == ConnectionState.Closed)
            await conn.OpenAsync(ct);

        if (profileType == ProfileType.Provider)
            return await CalculateProviderAsync(conn, profileId, ct);

        if (profileType == ProfileType.Participant || profileType == ProfileType.Owner)
            return await CalculateParticipantAsync(conn, profileId, profileType, ct);

        // Unknown profile type — return cold-start baseline
        return BuildColdStartResult(profileId, profileType, 0, isUnsupportedType: true);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Provider calculation (Phase 19 — unchanged)
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<ProfileScoreCalculationResult> CalculateProviderAsync(
        DbConnection conn, long profileId, CancellationToken ct)
    {
        var srMetrics = await GetServiceRequestMetricsAsync(conn, profileId, ct);
        var cdMetrics = await GetCargoDryMetricsAsync(conn, profileId, ct);
        var odMetrics = await GetOperationalDisciplineMetricsAsync(conn, profileId, ct);
        var frMetrics = await GetFinancialReliabilityMetricsAsync(conn, profileId, ct);
        var pcMetrics = await GetPlatformComplianceMetricsAsync(conn, profileId, ProfileType.Provider, ct);

        int sampleSize = srMetrics.TotalAssignments;
        if (sampleSize < ColdStartThreshold)
            return BuildColdStartResult(profileId, ProfileType.Provider, sampleSize);

        decimal srScore = ComputeServiceRequestScore(srMetrics);
        decimal cdScore = ComputeCargoDryScore(cdMetrics);
        decimal odScore = ComputeOperationalDisciplineScore(odMetrics);
        decimal frScore = ComputeFinancialReliabilityScore(frMetrics);
        decimal pcScore = ComputePlatformComplianceScore(pcMetrics);

        const decimal riskPenalty = 0m; // Applied by handler from active risk signals

        decimal overall = Math.Clamp(
            srScore * 0.35m
          + cdScore * 0.25m
          + odScore * 0.15m
          + frScore * 0.15m
          + pcScore * 0.10m
          - riskPenalty,
            0m, 100m);

        decimal confidence     = Math.Min(1.0m, sampleSize / (decimal)FullSampleSize);
        var     confidenceLevel = ClassifyConfidence(confidence);
        var     tier            = ClassifyTier(overall, confidence);

        var now        = DateTime.UtcNow;
        var components = new List<ProfileScoreComponentDto>
        {
            MakeComponent(PerformanceScoreCategory.ServiceRequest,        srScore, 0.35m, now, srMetrics.MetricsJson),
            MakeComponent(PerformanceScoreCategory.CargoDry,              cdScore, 0.25m, now, cdMetrics.MetricsJson),
            MakeComponent(PerformanceScoreCategory.OperationalDiscipline, odScore, 0.15m, now, odMetrics.MetricsJson),
            MakeComponent(PerformanceScoreCategory.FinancialReliability,  frScore, 0.15m, now, frMetrics.MetricsJson),
            MakeComponent(PerformanceScoreCategory.PlatformCompliance,    pcScore, 0.10m, now, pcMetrics.MetricsJson),
        };

        var metadata = new Dictionary<string, object?>
        {
            ["coldStart"]          = (object?)false,
            ["calculationVersion"] = CalculationVersion,
            ["profileType"]        = ProfileType.Provider.ToString(),
            ["sampleSize"]         = sampleSize,
        };

        return new ProfileScoreCalculationResult
        {
            ProfileId                  = profileId,
            ProfileType                = ProfileType.Provider,
            ServiceRequestScore        = srScore,
            CargoDryScore              = cdScore,
            OperationalDisciplineScore = odScore,
            FinancialReliabilityScore  = frScore,
            PlatformComplianceScore    = pcScore,
            RiskPenaltyScore           = riskPenalty,
            OverallScore               = overall,
            ConfidenceScore            = confidence,
            ConfidenceLevel            = confidenceLevel,
            SampleSize                 = sampleSize,
            IsColdStart                = false,
            DerivedTier                = tier,
            Components                 = components,
            MetadataJson               = JsonSerializer.Serialize(metadata),
            CalculatedAtUtc            = now,
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Participant / Owner calculation (Phase 22)
    //
    // Hard rules enforced here:
    // - No automatic restriction, demotion, or ranking of participants.
    // - Score is admin visibility signal only.
    // - Formula: SR*0.35 + CD*0.20 + FR*0.15 + PC*0.20 (OD not applicable → neutral).
    // - Cold-start: SampleSize < 3.
    // - Identity resolution: profileId → UserProfiles.UserId → used in cross-schema queries.
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<ProfileScoreCalculationResult> CalculateParticipantAsync(
        DbConnection conn, long profileId, ProfileType profileType, CancellationToken ct)
    {
        // ── Resolve UserId from ProfileId ─────────────────────────────────────
        // UserProfiles table (Identity module) has no schema prefix (default/public schema).
        long? userId = await ResolveParticipantUserIdAsync(conn, profileId, ct);
        if (!userId.HasValue)
        {
            // Profile not found in UserProfiles — return cold-start
            var metadata0 = new Dictionary<string, object?>
            {
                ["coldStart"]          = (object?)true,
                ["calculationVersion"] = CalculationVersion,
                ["profileType"]        = profileType.ToString(),
                ["sampleSize"]         = 0,
                ["participantRiskContext"] = true,
                ["note"]               = "UserProfile not found for profileId",
            };
            return BuildColdStartResult(profileId, profileType, 0,
                metadataOverride: JsonSerializer.Serialize(metadata0));
        }

        var srMetrics = await GetParticipantSrMetricsAsync(conn, userId.Value, ct);
        var cdMetrics = await GetParticipantCdMetricsAsync(conn, userId.Value, ct);
        var frMetrics = await GetParticipantFrMetricsAsync(conn, userId.Value, ct);
        var pcMetrics = await GetPlatformComplianceMetricsAsync(conn, profileId, profileType, ct);

        // ── Sample size for participants = total SRs created (not just completed) ──
        int sampleSize = srMetrics.TotalSrs;
        if (sampleSize < ParticipantColdStartThreshold)
        {
            var coldMeta = new Dictionary<string, object?>
            {
                ["coldStart"]           = (object?)true,
                ["calculationVersion"]  = CalculationVersion,
                ["profileType"]         = profileType.ToString(),
                ["sampleSize"]          = sampleSize,
                ["participantContext"]  = true,
            };
            return BuildColdStartResult(profileId, profileType, sampleSize,
                metadataOverride: JsonSerializer.Serialize(coldMeta));
        }

        decimal srScore = ComputeParticipantServiceRequestScore(srMetrics);
        decimal cdScore = ComputeParticipantCargoDryScore(cdMetrics);
        decimal odScore = NeutralScore;  // Not applicable for participants — always neutral
        decimal frScore = ComputeParticipantFinancialReliabilityScore(frMetrics);
        decimal pcScore = ComputePlatformComplianceScore(pcMetrics);

        const decimal riskPenalty = 0m; // Applied by handler from active risk signals

        // Participant formula: SR*0.35 + CD*0.20 + FR*0.15 + PC*0.20
        // OD excluded → max reachable = 90 before risk (reflects limited operational data for participants)
        decimal overall = Math.Clamp(
            srScore * 0.35m
          + cdScore * 0.20m
          // OD not included for participants
          + frScore * 0.15m
          + pcScore * 0.20m
          - riskPenalty,
            0m, 100m);

        decimal confidence      = Math.Min(1.0m, sampleSize / (decimal)FullSampleSize);
        var     confidenceLevel = ClassifyConfidence(confidence);
        var     tier            = ClassifyTier(overall, confidence);

        var now        = DateTime.UtcNow;
        var components = new List<ProfileScoreComponentDto>
        {
            MakeComponent(PerformanceScoreCategory.ServiceRequest,        srScore, 0.35m, now, srMetrics.MetricsJson),
            MakeComponent(PerformanceScoreCategory.CargoDry,              cdScore, 0.20m, now, cdMetrics.MetricsJson),
            MakeComponent(PerformanceScoreCategory.OperationalDiscipline, odScore, 0.00m, now, null),  // neutral, not applicable
            MakeComponent(PerformanceScoreCategory.FinancialReliability,  frScore, 0.15m, now, frMetrics.MetricsJson),
            MakeComponent(PerformanceScoreCategory.PlatformCompliance,    pcScore, 0.20m, now, pcMetrics.MetricsJson),
        };

        var metaDict = new Dictionary<string, object?>
        {
            ["coldStart"]            = (object?)false,
            ["calculationVersion"]   = CalculationVersion,
            ["profileType"]          = profileType.ToString(),
            ["sampleSize"]           = sampleSize,
            ["participantContext"]   = true,
            ["userId"]               = userId.Value,
            ["overdueInvoices"]      = frMetrics.OverdueInvoices,
            ["adminVisibilityOnly"]  = true,
        };

        return new ProfileScoreCalculationResult
        {
            ProfileId                  = profileId,
            ProfileType                = profileType,
            ServiceRequestScore        = srScore,
            CargoDryScore              = cdScore,
            OperationalDisciplineScore = odScore,
            FinancialReliabilityScore  = frScore,
            PlatformComplianceScore    = pcScore,
            RiskPenaltyScore           = riskPenalty,
            OverallScore               = overall,
            ConfidenceScore            = confidence,
            ConfidenceLevel            = confidenceLevel,
            SampleSize                 = sampleSize,
            IsColdStart                = false,
            DerivedTier                = tier,
            Components                 = components,
            MetadataJson               = JsonSerializer.Serialize(metaDict),
            CalculatedAtUtc            = now,
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Cold-start baseline
    // ─────────────────────────────────────────────────────────────────────────

    private static ProfileScoreCalculationResult BuildColdStartResult(
        long        profileId,
        ProfileType profileType,
        int         sampleSize,
        bool        isUnsupportedType  = false,
        string?     metadataOverride   = null)
    {
        var now        = DateTime.UtcNow;
        var components = Enum.GetValues<PerformanceScoreCategory>()
            .Select(cat => MakeComponent(cat, NeutralScore, WeightFor(cat, profileType), now, null))
            .ToList();

        var metadata = metadataOverride ?? JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["coldStart"]          = (object?)true,
            ["calculationVersion"] = CalculationVersion,
            ["profileType"]        = profileType.ToString(),
            ["sampleSize"]         = sampleSize,
            ["unsupportedType"]    = isUnsupportedType,
        });

        return new ProfileScoreCalculationResult
        {
            ProfileId                  = profileId,
            ProfileType                = profileType,
            ServiceRequestScore        = NeutralScore,
            CargoDryScore              = NeutralScore,
            OperationalDisciplineScore = NeutralScore,
            FinancialReliabilityScore  = NeutralScore,
            PlatformComplianceScore    = NeutralScore,
            RiskPenaltyScore           = 0m,
            OverallScore               = NeutralScore,
            ConfidenceScore            = 0.10m,
            ConfidenceLevel            = PerformanceConfidenceLevel.ColdStart,
            SampleSize                 = sampleSize,
            IsColdStart                = true,
            DerivedTier                = PriorityTier.Standard,
            Components                 = components,
            MetadataJson               = metadata,
            CalculatedAtUtc            = now,
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Participant identity resolution
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Resolves the Identity UserId for a given ProfileId.
    /// UserProfiles table belongs to the Identity module — no schema prefix (default/public schema).
    /// Result is used to query cross-module tables that store UserId (not ProfileId).
    /// </summary>
    private static async Task<long?> ResolveParticipantUserIdAsync(
        DbConnection conn, long profileId, CancellationToken ct)
    {
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT ""UserId"" FROM ""UserProfiles"" WHERE ""Id"" = @profileId LIMIT 1";
            Param(cmd, "profileId", profileId);

            var result = await cmd.ExecuteScalarAsync(ct);
            return result is null || result == DBNull.Value ? null : Convert.ToInt64(result);
        }
        catch
        {
            return null;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Provider cross-schema raw SQL readers (Phase 19 — unchanged)
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<SrMetrics> GetServiceRequestMetricsAsync(
        DbConnection conn, long profileId, CancellationToken ct)
    {
        var m     = new SrMetrics();
        try
        {
            var since = DateTime.UtcNow.AddMonths(-12);

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT
                        COUNT(*)                                                   AS ""TotalAssignments"",
                        COUNT(*) FILTER (WHERE sr.""Status"" = @srCompleted)       AS ""CompletedAssignments"",
                        COUNT(*) FILTER (
                            WHERE a.""CreateDate"" IS NOT NULL
                              AND sr.""CreateDate"" IS NOT NULL
                              AND (a.""CreateDate"" - sr.""CreateDate"") < INTERVAL '2 hours'
                        )                                                          AS ""ResponsesUnder2h"",
                        COUNT(*) FILTER (
                            WHERE a.""CreateDate"" IS NOT NULL
                              AND sr.""CreateDate"" IS NOT NULL
                              AND (a.""CreateDate"" - sr.""CreateDate"") < INTERVAL '6 hours'
                        )                                                          AS ""ResponsesUnder6h""
                    FROM servicerequest.service_request_assignments a
                    JOIN servicerequest.service_requests sr
                        ON sr.""Id"" = a.""ServiceRequestId"" AND sr.""IsDeleted"" = false
                    WHERE a.""ProviderProfileId"" = @profileId
                      AND a.""IsDeleted"" = false
                      AND a.""CreateDate"" >= @since";
                Param(cmd, "profileId",    profileId);
                Param(cmd, "srCompleted",  SrStatusCompleted);
                Param(cmd, "since",        since);

                using var r = await cmd.ExecuteReaderAsync(ct);
                if (await r.ReadAsync(ct))
                {
                    m.TotalAssignments     = ToInt(r["TotalAssignments"]);
                    m.CompletedAssignments = ToInt(r["CompletedAssignments"]);
                    m.ResponsesUnder2h     = ToInt(r["ResponsesUnder2h"]);
                    m.ResponsesUnder6h     = ToInt(r["ResponsesUnder6h"]);
                }
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT
                        COUNT(*) FILTER (WHERE c.""ClientRating"" IS NOT NULL)    AS ""RatedCompletions"",
                        COUNT(*) FILTER (WHERE c.""ClientRating"" >= 4)           AS ""HighRatedCompletions""
                    FROM servicerequest.service_request_completions c
                    JOIN servicerequest.service_request_assignments a
                        ON a.""ServiceRequestId"" = c.""ServiceRequestId""
                           AND a.""IsDeleted"" = false
                    WHERE a.""ProviderProfileId"" = @profileId
                      AND c.""IsDeleted"" = false
                      AND c.""CreateDate"" >= @since";
                Param(cmd, "profileId", profileId);
                Param(cmd, "since",     since);

                using var r = await cmd.ExecuteReaderAsync(ct);
                if (await r.ReadAsync(ct))
                {
                    m.RatedCompletions     = ToInt(r["RatedCompletions"]);
                    m.HighRatedCompletions = ToInt(r["HighRatedCompletions"]);
                }
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT
                        COUNT(*)                                                   AS ""TotalDisputes"",
                        COUNT(*) FILTER (
                            WHERE d.""Status"" = @resolved
                            AND   d.""ResolutionNotes"" ILIKE '%provider%'
                        )                                                          AS ""WonDisputes""
                    FROM servicerequest.service_request_disputes d
                    JOIN servicerequest.service_request_assignments a
                        ON a.""ServiceRequestId"" = d.""ServiceRequestId""
                           AND a.""IsDeleted"" = false
                    WHERE a.""ProviderProfileId"" = @profileId
                      AND d.""IsDeleted"" = false
                      AND d.""CreateDate"" >= @since";
                Param(cmd, "profileId", profileId);
                Param(cmd, "resolved",  SrDisputeStatusResolved);
                Param(cmd, "since",     since);

                using var r = await cmd.ExecuteReaderAsync(ct);
                if (await r.ReadAsync(ct))
                {
                    m.TotalDisputes = ToInt(r["TotalDisputes"]);
                    m.WonDisputes   = ToInt(r["WonDisputes"]);
                }
            }
        }
        catch { /* Graceful degradation — partial data returns neutral scores */ }

        m.MetricsJson = Serialize(new
        {
            totalAssignments    = m.TotalAssignments,
            completed           = m.CompletedAssignments,
            responseUnder2h     = m.ResponsesUnder2h,
            responseUnder6h     = m.ResponsesUnder6h,
            ratedCompletions    = m.RatedCompletions,
            highRated           = m.HighRatedCompletions,
            totalDisputes       = m.TotalDisputes,
            wonDisputes         = m.WonDisputes,
        });
        return m;
    }

    private async Task<CdMetrics> GetCargoDryMetricsAsync(
        DbConnection conn, long profileId, CancellationToken ct)
    {
        var m = new CdMetrics();
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    COUNT(*)                                                           AS ""AssignedKits"",
                    COUNT(*) FILTER (
                        WHERE ""Status"" IN (@activated, @expired, @renewed)
                    )                                                                  AS ""ActivatedKits"",
                    COUNT(*) FILTER (WHERE ""Status"" IN (@expired, @renewed))         AS ""KitsWithUsage"",
                    COUNT(*) FILTER (WHERE ""Status"" = @renewed)                      AS ""RenewedKits"",
                    COUNT(*) FILTER (WHERE ""Status"" IN (@expired, @renewed))         AS ""EligibleForRenewal""
                FROM cargodry.kits
                WHERE ""ProviderProfileId"" = @profileId
                  AND ""IsDeleted"" = false";
            Param(cmd, "profileId",  profileId);
            Param(cmd, "activated",  KitStatusActivated);
            Param(cmd, "expired",    KitStatusExpired);
            Param(cmd, "renewed",    KitStatusRenewed);

            using var r = await cmd.ExecuteReaderAsync(ct);
            if (await r.ReadAsync(ct))
            {
                m.AssignedKits       = ToInt(r["AssignedKits"]);
                m.ActivatedKits      = ToInt(r["ActivatedKits"]);
                m.KitsWithUsage      = ToInt(r["KitsWithUsage"]);
                m.RenewedKits        = ToInt(r["RenewedKits"]);
                m.EligibleForRenewal = ToInt(r["EligibleForRenewal"]);
            }
        }
        catch { /* Graceful degradation */ }

        m.MetricsJson = Serialize(new
        {
            assignedKits       = m.AssignedKits,
            activatedKits      = m.ActivatedKits,
            kitsWithUsage      = m.KitsWithUsage,
            renewedKits        = m.RenewedKits,
            eligibleForRenewal = m.EligibleForRenewal,
        });
        return m;
    }

    private async Task<OdMetrics> GetOperationalDisciplineMetricsAsync(
        DbConnection conn, long profileId, CancellationToken ct)
    {
        var m = new OdMetrics();
        try
        {
            var since = DateTime.UtcNow.AddMonths(-12);

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT
                        COUNT(*)                                                    AS ""TotalAssignments"",
                        COUNT(*) FILTER (WHERE ""ScheduledStartDate"" IS NOT NULL) AS ""TotalScheduledStarts"",
                        COUNT(*) FILTER (
                            WHERE ""ScheduledStartDate"" IS NOT NULL
                              AND ""ActualStartDate""    IS NOT NULL
                              AND ""ActualStartDate"" <= ""ScheduledStartDate"" + INTERVAL '2 hours'
                        )                                                           AS ""OnTimeStarts""
                    FROM servicerequest.service_request_assignments
                    WHERE ""ProviderProfileId"" = @profileId
                      AND ""IsDeleted"" = false
                      AND ""CreateDate"" >= @since";
                Param(cmd, "profileId", profileId);
                Param(cmd, "since",     since);

                using var r = await cmd.ExecuteReaderAsync(ct);
                if (await r.ReadAsync(ct))
                {
                    m.TotalAssignments     = ToInt(r["TotalAssignments"]);
                    m.TotalScheduledStarts = ToInt(r["TotalScheduledStarts"]);
                    m.OnTimeStarts         = ToInt(r["OnTimeStarts"]);
                }
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT
                        COUNT(*)                                                       AS ""TotalPhases"",
                        COUNT(*) FILTER (WHERE wp.""Status"" = 'Completed')           AS ""CompletedPhases""
                    FROM servicerequest.service_request_work_phases wp
                    JOIN servicerequest.service_request_assignments a
                        ON a.""ServiceRequestId"" = wp.""ServiceRequestId""
                           AND a.""IsDeleted"" = false
                    WHERE a.""ProviderProfileId"" = @profileId
                      AND wp.""IsDeleted"" = false
                      AND wp.""CreateDate"" >= @since";
                Param(cmd, "profileId", profileId);
                Param(cmd, "since",     since);

                using var r = await cmd.ExecuteReaderAsync(ct);
                if (await r.ReadAsync(ct))
                {
                    m.TotalPhases     = ToInt(r["TotalPhases"]);
                    m.CompletedPhases = ToInt(r["CompletedPhases"]);
                }
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT COUNT(DISTINCT c.""ServiceRequestId"") AS ""SrsWithDocuments""
                    FROM servicerequest.service_request_completions c
                    JOIN servicerequest.service_request_assignments a
                        ON a.""ServiceRequestId"" = c.""ServiceRequestId""
                           AND a.""IsDeleted"" = false
                    WHERE a.""ProviderProfileId"" = @profileId
                      AND c.""IsDeleted"" = false
                      AND c.""CreateDate"" >= @since";
                Param(cmd, "profileId", profileId);
                Param(cmd, "since",     since);

                using var r = await cmd.ExecuteReaderAsync(ct);
                if (await r.ReadAsync(ct))
                    m.SrsWithDocuments = ToInt(r["SrsWithDocuments"]);
            }
        }
        catch { /* Graceful degradation */ }

        m.MetricsJson = Serialize(new
        {
            totalScheduledStarts = m.TotalScheduledStarts,
            onTimeStarts         = m.OnTimeStarts,
            totalPhases          = m.TotalPhases,
            completedPhases      = m.CompletedPhases,
            srsWithDocuments     = m.SrsWithDocuments,
            totalAssignments     = m.TotalAssignments,
        });
        return m;
    }

    private async Task<FrMetrics> GetFinancialReliabilityMetricsAsync(
        DbConnection conn, long profileId, CancellationToken ct)
    {
        var m = new FrMetrics();
        try
        {
            var since = DateTime.UtcNow.AddMonths(-12);

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT
                        COUNT(*)                                               AS ""TotalPayouts"",
                        COUNT(*) FILTER (WHERE ""Status"" = @failed)          AS ""FailedPayouts""
                    FROM payment.payout_records
                    WHERE ""ProviderProfileId"" = @profileId
                      AND ""IsDeleted""         = false
                      AND ""CreateDate""        >= @since";
                Param(cmd, "profileId", profileId);
                Param(cmd, "failed",    PayoutStatusFailed);
                Param(cmd, "since",     since);

                using var r = await cmd.ExecuteReaderAsync(ct);
                if (await r.ReadAsync(ct))
                {
                    m.TotalPayouts  = ToInt(r["TotalPayouts"]);
                    m.FailedPayouts = ToInt(r["FailedPayouts"]);
                }
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT
                        COUNT(*)                                                         AS ""TotalPayouts"",
                        COUNT(*) FILTER (
                            WHERE EXISTS (
                                SELECT 1 FROM payment.invoice_headers ih
                                WHERE ih.""ProviderPayoutId"" = pr.""Id""
                                  AND ih.""IsDeleted"" = false
                            )
                        )                                                                AS ""InvoicedPayouts""
                    FROM payment.payout_records pr
                    WHERE pr.""ProviderProfileId"" = @profileId
                      AND pr.""IsDeleted""         = false
                      AND pr.""CreateDate""        >= @since";
                Param(cmd, "profileId", profileId);
                Param(cmd, "since",     since);

                using var r = await cmd.ExecuteReaderAsync(ct);
                if (await r.ReadAsync(ct))
                {
                    m.TotalInvoiceableEvents = ToInt(r["TotalPayouts"]);
                    m.IssuedInvoices         = ToInt(r["InvoicedPayouts"]);
                }
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT
                        COUNT(*)                                               AS ""TotalTransactions"",
                        COUNT(*) FILTER (WHERE ""Status"" = @disputed)        AS ""CommissionConflicts""
                    FROM payment.transactions
                    WHERE ""RecipientProfileId"" = @profileId
                      AND ""IsDeleted""          = false
                      AND ""CreateDate""         >= @since";
                Param(cmd, "profileId", profileId);
                Param(cmd, "disputed",  TxStatusDisputed);
                Param(cmd, "since",     since);

                using var r = await cmd.ExecuteReaderAsync(ct);
                if (await r.ReadAsync(ct))
                {
                    m.TotalTransactions   = ToInt(r["TotalTransactions"]);
                    m.CommissionConflicts = ToInt(r["CommissionConflicts"]);
                }
            }
        }
        catch { /* Graceful degradation */ }

        m.MetricsJson = Serialize(new
        {
            totalPayouts           = m.TotalPayouts,
            failedPayouts          = m.FailedPayouts,
            totalInvoiceableEvents = m.TotalInvoiceableEvents,
            issuedInvoices         = m.IssuedInvoices,
            totalTransactions      = m.TotalTransactions,
            commissionConflicts    = m.CommissionConflicts,
        });
        return m;
    }

    /// <summary>
    /// Platform compliance metrics — generic for any profile type.
    /// Reads from profile.profile_metric_caches (populated by admin or external triggers).
    /// </summary>
    private async Task<PcMetrics> GetPlatformComplianceMetricsAsync(
        DbConnection conn, long profileId, ProfileType profileType, CancellationToken ct)
    {
        var m = new PcMetrics
        {
            ProfileCompletionScore = 50m,
            IsIdVerified           = false,
            TosViolationCount      = 0,
        };

        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT ""MetricKey"", ""MetricValueJson""
                FROM profile.profile_metric_caches
                WHERE ""ProfileId""   = @profileId
                  AND ""ProfileType"" = @profileType
                  AND ""IsDeleted""   = false
                  AND (""ExpiresAtUtc"" IS NULL OR ""ExpiresAtUtc"" > NOW())
                  AND ""MetricKey"" IN (
                      'profile_completion_score',
                      'id_verification_status',
                      'tos_violation_count'
                  )";
            Param(cmd, "profileId",   profileId);
            Param(cmd, "profileType", (int)profileType);

            using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                var key   = r["MetricKey"]?.ToString()      ?? "";
                var value = r["MetricValueJson"]?.ToString() ?? "null";

                switch (key)
                {
                    case "profile_completion_score":
                        if (decimal.TryParse(value.Trim('"'), out var cs))
                            m.ProfileCompletionScore = cs;
                        break;
                    case "id_verification_status":
                        m.IsIdVerified = value.Contains("true", StringComparison.OrdinalIgnoreCase);
                        break;
                    case "tos_violation_count":
                        if (int.TryParse(value.Trim('"'), out var vc))
                            m.TosViolationCount = vc;
                        break;
                }
            }
        }
        catch { /* Graceful degradation */ }

        m.MetricsJson = Serialize(new
        {
            profileCompletionScore = m.ProfileCompletionScore,
            isIdVerified           = m.IsIdVerified,
            tosViolationCount      = m.TosViolationCount,
        });
        return m;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Participant cross-schema raw SQL readers (Phase 22)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Phase 22 — Participant SR behaviour metrics.
    /// Queries service_requests by OwnerUserId (not ProviderProfileId).
    /// Metrics: total SRs, cancellation rate, dispute rate, completion rate.
    /// PAR_SR_COMPLETION_APPROVAL_DELAY deferred to post-MVP (requires status history analysis).
    /// Admin visibility signal only — does not affect assignment or routing.
    /// </summary>
    private async Task<ParticipantSrMetrics> GetParticipantSrMetricsAsync(
        DbConnection conn, long userId, CancellationToken ct)
    {
        var m = new ParticipantSrMetrics();
        try
        {
            var since = DateTime.UtcNow.AddMonths(-12);

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    COUNT(*)                                                             AS ""TotalSrs"",
                    COUNT(*) FILTER (WHERE ""Status"" = @completed)                    AS ""CompletedSrs"",
                    COUNT(*) FILTER (WHERE ""Status"" = @cancelled)                    AS ""CancelledSrs"",
                    COUNT(*) FILTER (
                        WHERE ""Status"" IN (@disputeOpened, @underDisputeReview, @disputeResolved)
                    )                                                                    AS ""DisputeSrs"",
                    COUNT(*) FILTER (
                        WHERE ""CancelledByUserId"" = @userId
                          AND ""Status""             = @cancelled
                    )                                                                    AS ""SelfCancelledSrs""
                FROM servicerequest.service_requests
                WHERE ""OwnerUserId"" = @userId
                  AND ""IsDeleted""   = false
                  AND ""CreateDate""  >= @since";
            Param(cmd, "userId",             userId);
            Param(cmd, "completed",          SrStatusCompleted);
            Param(cmd, "cancelled",          SrStatusCancelled);
            Param(cmd, "disputeOpened",      SrStatusDisputeOpened);
            Param(cmd, "underDisputeReview", SrStatusUnderDisputeReview);
            Param(cmd, "disputeResolved",    SrStatusDisputeResolved);
            Param(cmd, "since",              since);

            using var r = await cmd.ExecuteReaderAsync(ct);
            if (await r.ReadAsync(ct))
            {
                m.TotalSrs       = ToInt(r["TotalSrs"]);
                m.CompletedSrs   = ToInt(r["CompletedSrs"]);
                m.CancelledSrs   = ToInt(r["CancelledSrs"]);
                m.DisputeSrs     = ToInt(r["DisputeSrs"]);
                m.SelfCancelledSrs = ToInt(r["SelfCancelledSrs"]);
            }
        }
        catch { /* Graceful degradation */ }

        m.MetricsJson = Serialize(new
        {
            totalSrs        = m.TotalSrs,
            completedSrs    = m.CompletedSrs,
            cancelledSrs    = m.CancelledSrs,
            disputeSrs      = m.DisputeSrs,
            selfCancelled   = m.SelfCancelledSrs,
            approvalDelayDeferredPostMvp = true,
        });
        return m;
    }

    /// <summary>
    /// Phase 22 — Participant CargoDry renewal behaviour metrics.
    /// Queries cargodry.kits and cargodry.renewal_preparations by OwnerUserId.
    /// Captures: kit activation by owner, renewal preparation completion rate.
    /// Admin visibility signal only — does not affect CargoDry routing.
    /// </summary>
    private async Task<ParticipantCdMetrics> GetParticipantCdMetricsAsync(
        DbConnection conn, long userId, CancellationToken ct)
    {
        var m = new ParticipantCdMetrics();
        try
        {
            // Owner's kits: activation and renewal behaviour
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT
                        COUNT(*)                                                           AS ""TotalOwnerKits"",
                        COUNT(*) FILTER (
                            WHERE ""Status"" IN (@activated, @expired, @renewed)
                        )                                                                  AS ""ActivatedOwnerKits"",
                        COUNT(*) FILTER (WHERE ""Status"" = @renewed)                      AS ""RenewedOwnerKits"",
                        COUNT(*) FILTER (WHERE ""Status"" IN (@expired, @renewed))         AS ""EligibleOwnerRenewal""
                    FROM cargodry.kits
                    WHERE ""OwnerUserId"" = @userId
                      AND ""IsDeleted""   = false";
                Param(cmd, "userId",     userId);
                Param(cmd, "activated",  KitStatusActivated);
                Param(cmd, "expired",    KitStatusExpired);
                Param(cmd, "renewed",    KitStatusRenewed);

                using var r = await cmd.ExecuteReaderAsync(ct);
                if (await r.ReadAsync(ct))
                {
                    m.TotalOwnerKits      = ToInt(r["TotalOwnerKits"]);
                    m.ActivatedOwnerKits  = ToInt(r["ActivatedOwnerKits"]);
                    m.RenewedOwnerKits    = ToInt(r["RenewedOwnerKits"]);
                    m.EligibleOwnerRenewal = ToInt(r["EligibleOwnerRenewal"]);
                }
            }

            // Renewal preparations by OwnerUserId
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT
                        COUNT(*)                                                 AS ""TotalPreparations"",
                        COUNT(*) FILTER (WHERE ""Status"" = 3 OR ""Status"" = 4) AS ""CompletedPreparations""
                    FROM cargodry.renewal_preparations
                    WHERE ""OwnerUserId"" = @userId
                      AND ""IsDeleted""   = false";
                // Status 3 = Completed, 4 = Paid (both indicate completed renewal flow)
                Param(cmd, "userId", userId);

                using var r = await cmd.ExecuteReaderAsync(ct);
                if (await r.ReadAsync(ct))
                {
                    m.TotalPreparations     = ToInt(r["TotalPreparations"]);
                    m.CompletedPreparations = ToInt(r["CompletedPreparations"]);
                }
            }
        }
        catch { /* Graceful degradation */ }

        m.MetricsJson = Serialize(new
        {
            totalOwnerKits         = m.TotalOwnerKits,
            activatedOwnerKits     = m.ActivatedOwnerKits,
            renewedOwnerKits       = m.RenewedOwnerKits,
            eligibleOwnerRenewal   = m.EligibleOwnerRenewal,
            totalPreparations      = m.TotalPreparations,
            completedPreparations  = m.CompletedPreparations,
        });
        return m;
    }

    /// <summary>
    /// Phase 22 — Participant financial reliability metrics.
    /// Queries payment.invoice_headers by BuyerUserId.
    /// Captures: total buyer invoices, overdue invoice count.
    /// PAR_INVOICE_PAYMENT_DELAY_DAYS deferred — requires PaidAtUtc on InvoiceHeaderEntity (out of scope).
    /// Admin visibility signal only — no payment enforcement.
    /// </summary>
    private async Task<ParticipantFrMetrics> GetParticipantFrMetricsAsync(
        DbConnection conn, long userId, CancellationToken ct)
    {
        var m = new ParticipantFrMetrics();
        try
        {
            var since = DateTime.UtcNow.AddMonths(-24); // 24-month window for invoice context

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    COUNT(*)                                                             AS ""TotalInvoices"",
                    COUNT(*) FILTER (WHERE ""Status"" = @overdue)                       AS ""OverdueInvoices"",
                    COUNT(*) FILTER (WHERE ""Status"" IN (3, 4, 5))                     AS ""ActiveInvoices""
                FROM payment.invoice_headers
                WHERE ""BuyerUserId"" = @userId
                  AND ""IsDeleted""   = false
                  AND ""CreateDate""  >= @since";
            // Status 3=Sent, 4=Paid, 5=PartiallyPaid, 6=Overdue
            Param(cmd, "userId",   userId);
            Param(cmd, "overdue",  InvoiceStatusOverdue);
            Param(cmd, "since",    since);

            using var r = await cmd.ExecuteReaderAsync(ct);
            if (await r.ReadAsync(ct))
            {
                m.TotalInvoices   = ToInt(r["TotalInvoices"]);
                m.OverdueInvoices = ToInt(r["OverdueInvoices"]);
                m.ActiveInvoices  = ToInt(r["ActiveInvoices"]);
            }
        }
        catch { /* Graceful degradation */ }

        m.MetricsJson = Serialize(new
        {
            totalInvoices           = m.TotalInvoices,
            overdueInvoices         = m.OverdueInvoices,
            activeInvoices          = m.ActiveInvoices,
            paymentDelayDeferredPostMvp = true,
        });
        return m;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Provider score computation (Phase 19 — unchanged)
    // ─────────────────────────────────────────────────────────────────────────

    private static decimal ComputeServiceRequestScore(SrMetrics m)
    {
        decimal completionRate   = Rate(m.CompletedAssignments, m.TotalAssignments);
        decimal responseScore    = Rate(m.ResponsesUnder2h, m.TotalAssignments) * 0.60m
                                 + Rate(m.ResponsesUnder6h, m.TotalAssignments) * 0.40m;
        decimal satisfactionRate = Rate(m.HighRatedCompletions, m.RatedCompletions);
        decimal disputeRate      = Rate(m.TotalDisputes, m.CompletedAssignments);
        decimal disputeScore     = Math.Max(0m, 100m - disputeRate * 2m);
        decimal winRate          = Rate(m.WonDisputes, m.TotalDisputes);

        return Math.Clamp(
              completionRate   * 0.30m
            + responseScore    * 0.20m
            + satisfactionRate * 0.25m
            + disputeScore     * 0.15m
            + winRate          * 0.10m,
            0m, 100m);
    }

    private static decimal ComputeCargoDryScore(CdMetrics m)
    {
        decimal activationRate  = Rate(m.ActivatedKits,    m.AssignedKits);
        decimal sellThroughRate = Rate(m.KitsWithUsage,    m.ActivatedKits);
        decimal renewalRate     = Rate(m.RenewedKits,      m.EligibleForRenewal);

        return Math.Clamp(
              activationRate  * 0.45m
            + sellThroughRate * 0.35m
            + renewalRate     * 0.20m,
            0m, 100m);
    }

    private static decimal ComputeOperationalDisciplineScore(OdMetrics m)
    {
        decimal onTimeRate     = Rate(m.OnTimeStarts,     m.TotalScheduledStarts);
        decimal phaseAdherence = Rate(m.CompletedPhases,  m.TotalPhases);
        decimal docUploadRate  = Rate(m.SrsWithDocuments, m.TotalAssignments);

        return Math.Clamp(
              onTimeRate     * 0.35m
            + phaseAdherence * 0.40m
            + docUploadRate  * 0.25m,
            0m, 100m);
    }

    private static decimal ComputeFinancialReliabilityScore(FrMetrics m)
    {
        decimal payoutFailRate = Rate(m.FailedPayouts,       m.TotalPayouts);
        decimal payoutScore    = Math.Max(0m, 100m - payoutFailRate * 3m);
        decimal invoiceRate    = Rate(m.IssuedInvoices,      m.TotalInvoiceableEvents);
        decimal conflictRate   = Rate(m.CommissionConflicts, m.TotalTransactions);
        decimal conflictScore  = Math.Max(0m, 100m - conflictRate * 5m);

        return Math.Clamp(
              payoutScore   * 0.50m
            + invoiceRate   * 0.30m
            + conflictScore * 0.20m,
            0m, 100m);
    }

    private static decimal ComputePlatformComplianceScore(PcMetrics m)
    {
        decimal profileScore = Math.Clamp(m.ProfileCompletionScore, 0m, 100m);
        decimal idScore      = m.IsIdVerified ? 100m : 0m;
        decimal tosScore     = Math.Max(0m, 100m - m.TosViolationCount * 20m);

        return Math.Clamp(
              profileScore * 0.40m
            + idScore      * 0.40m
            + tosScore     * 0.20m,
            0m, 100m);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Participant score computation (Phase 22)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Participant SR behaviour score.
    /// Lower cancellation rate + lower dispute rate + higher completion rate → higher score.
    /// Admin visibility signal only — does not affect assignment or offer routing.
    /// </summary>
    private static decimal ComputeParticipantServiceRequestScore(ParticipantSrMetrics m)
    {
        // Cancellation rate (lower is better) — penalise self-cancellations heavily
        decimal cancelRate        = Rate(m.CancelledSrs, m.TotalSrs);
        decimal cancelScore       = Math.Max(0m, 100m - cancelRate * 1.5m);  // 67% cancel rate → 0

        // Dispute creation rate (lower is better)
        decimal disputeRate       = Rate(m.DisputeSrs, m.TotalSrs);
        decimal disputeScore      = Math.Max(0m, 100m - disputeRate * 2m);   // 50% dispute rate → 0

        // Completion rate (higher is better)
        decimal completionRate    = Rate(m.CompletedSrs, m.TotalSrs);

        return Math.Clamp(
              completionRate * 0.40m
            + cancelScore    * 0.35m
            + disputeScore   * 0.25m,
            0m, 100m);
    }

    /// <summary>
    /// Participant CargoDry renewal reliability score.
    /// Higher owner kit activation + renewal completion rate → higher score.
    /// Admin visibility signal only — does not affect CargoDry routing.
    /// </summary>
    private static decimal ComputeParticipantCargoDryScore(ParticipantCdMetrics m)
    {
        decimal activationRate   = Rate(m.ActivatedOwnerKits,     m.TotalOwnerKits);
        decimal renewalRate      = Rate(m.RenewedOwnerKits,       m.EligibleOwnerRenewal);
        decimal preparationRate  = Rate(m.CompletedPreparations,  m.TotalPreparations);

        // If no kits at all, return neutral
        if (m.TotalOwnerKits == 0 && m.TotalPreparations == 0)
            return NeutralScore;

        return Math.Clamp(
              activationRate  * 0.45m
            + renewalRate     * 0.35m
            + preparationRate * 0.20m,
            0m, 100m);
    }

    /// <summary>
    /// Participant financial reliability score.
    /// Fewer overdue invoices → higher score.
    /// PAR_INVOICE_PAYMENT_DELAY_DAYS deferred (requires PaidAtUtc on InvoiceHeaderEntity).
    /// Admin visibility signal only — no payment enforcement.
    /// </summary>
    private static decimal ComputeParticipantFinancialReliabilityScore(ParticipantFrMetrics m)
    {
        // If no invoices at all, return neutral
        if (m.TotalInvoices == 0)
            return NeutralScore;

        decimal overdueRate  = Rate(m.OverdueInvoices, m.TotalInvoices);
        // Heavy penalty for overdue invoices: 20% overdue rate → score ~0
        decimal overdueScore = Math.Max(0m, 100m - overdueRate * 5m);

        return Math.Clamp(overdueScore, 0m, 100m);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Classification helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static PriorityTier ClassifyTier(decimal overallScore, decimal confidence) =>
        (overallScore, confidence) switch
        {
            ( >= 90m, >= 0.70m) => PriorityTier.Platinum,
            ( >= 75m, >= 0.50m) => PriorityTier.Gold,
            ( >= 60m, >= 0.40m) => PriorityTier.Silver,
            _                   => PriorityTier.Standard,
        };

    private static PerformanceConfidenceLevel ClassifyConfidence(decimal c) => c switch
    {
        >= 0.80m => PerformanceConfidenceLevel.High,
        >= 0.60m => PerformanceConfidenceLevel.Medium,
        >= 0.40m => PerformanceConfidenceLevel.Low,
        _        => PerformanceConfidenceLevel.ColdStart,
    };

    // ─────────────────────────────────────────────────────────────────────────
    // Utility
    // ─────────────────────────────────────────────────────────────────────────

    private static decimal Rate(int numerator, int denominator)
        => denominator <= 0 ? NeutralScore : Math.Clamp(numerator * 100m / denominator, 0m, 100m);

    private static decimal WeightFor(PerformanceScoreCategory cat, ProfileType profileType)
    {
        if (profileType == ProfileType.Provider)
        {
            return cat switch
            {
                PerformanceScoreCategory.ServiceRequest        => 0.35m,
                PerformanceScoreCategory.CargoDry              => 0.25m,
                PerformanceScoreCategory.OperationalDiscipline => 0.15m,
                PerformanceScoreCategory.FinancialReliability  => 0.15m,
                PerformanceScoreCategory.PlatformCompliance    => 0.10m,
                _                                              => 0.00m,
            };
        }
        // Participant / Owner weights
        return cat switch
        {
            PerformanceScoreCategory.ServiceRequest        => 0.35m,
            PerformanceScoreCategory.CargoDry              => 0.20m,
            PerformanceScoreCategory.OperationalDiscipline => 0.00m,  // Not applicable
            PerformanceScoreCategory.FinancialReliability  => 0.15m,
            PerformanceScoreCategory.PlatformCompliance    => 0.20m,
            _                                              => 0.00m,
        };
    }

    private static ProfileScoreComponentDto MakeComponent(
        PerformanceScoreCategory category,
        decimal                  rawScore,
        decimal                  weight,
        DateTime                 calculatedAt,
        string?                  metricsJson)
    {
        var clamped = Math.Clamp(rawScore, 0m, 100m);
        return new ProfileScoreComponentDto
        {
            SnapshotId           = 0,
            Category             = category,
            RawScore             = clamped,
            Weight               = weight,
            WeightedContribution = Math.Round(clamped * weight, 4),
            MetricCount          = 0,
            MetricsJson          = metricsJson,
            CalculatedAtUtc      = calculatedAt,
        };
    }

    private static void Param(DbCommand cmd, string name, object? value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value         = value ?? DBNull.Value;
        cmd.Parameters.Add(p);
    }

    private static int ToInt(object? v)
        => v is null || v == DBNull.Value ? 0 : Convert.ToInt32(v);

    private static string Serialize(object obj)
        => JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = false });

    // ─────────────────────────────────────────────────────────────────────────
    // Private metric bags — Provider (Phase 19)
    // ─────────────────────────────────────────────────────────────────────────

    private sealed class SrMetrics
    {
        public int     TotalAssignments      { get; set; }
        public int     CompletedAssignments   { get; set; }
        public int     ResponsesUnder2h       { get; set; }
        public int     ResponsesUnder6h       { get; set; }
        public int     RatedCompletions       { get; set; }
        public int     HighRatedCompletions   { get; set; }
        public int     TotalDisputes          { get; set; }
        public int     WonDisputes            { get; set; }
        public string? MetricsJson            { get; set; }
    }

    private sealed class CdMetrics
    {
        public int     AssignedKits       { get; set; }
        public int     ActivatedKits      { get; set; }
        public int     KitsWithUsage      { get; set; }
        public int     RenewedKits        { get; set; }
        public int     EligibleForRenewal  { get; set; }
        public string? MetricsJson        { get; set; }
    }

    private sealed class OdMetrics
    {
        public int     TotalAssignments    { get; set; }
        public int     TotalScheduledStarts { get; set; }
        public int     OnTimeStarts         { get; set; }
        public int     TotalPhases          { get; set; }
        public int     CompletedPhases      { get; set; }
        public int     SrsWithDocuments     { get; set; }
        public string? MetricsJson          { get; set; }
    }

    private sealed class FrMetrics
    {
        public int     TotalPayouts           { get; set; }
        public int     FailedPayouts          { get; set; }
        public int     TotalInvoiceableEvents { get; set; }
        public int     IssuedInvoices         { get; set; }
        public int     TotalTransactions      { get; set; }
        public int     CommissionConflicts    { get; set; }
        public string? MetricsJson            { get; set; }
    }

    private sealed class PcMetrics
    {
        public decimal ProfileCompletionScore { get; set; }
        public bool    IsIdVerified           { get; set; }
        public int     TosViolationCount      { get; set; }
        public string? MetricsJson            { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private metric bags — Participant / Owner (Phase 22)
    // ─────────────────────────────────────────────────────────────────────────

    private sealed class ParticipantSrMetrics
    {
        public int     TotalSrs         { get; set; }
        public int     CompletedSrs     { get; set; }
        public int     CancelledSrs     { get; set; }
        public int     DisputeSrs       { get; set; }
        public int     SelfCancelledSrs { get; set; }
        public string? MetricsJson      { get; set; }
    }

    private sealed class ParticipantCdMetrics
    {
        public int     TotalOwnerKits       { get; set; }
        public int     ActivatedOwnerKits   { get; set; }
        public int     RenewedOwnerKits     { get; set; }
        public int     EligibleOwnerRenewal { get; set; }
        public int     TotalPreparations    { get; set; }
        public int     CompletedPreparations { get; set; }
        public string? MetricsJson          { get; set; }
    }

    private sealed class ParticipantFrMetrics
    {
        public int     TotalInvoices   { get; set; }
        public int     OverdueInvoices { get; set; }
        public int     ActiveInvoices  { get; set; }
        public string? MetricsJson     { get; set; }
    }
}
