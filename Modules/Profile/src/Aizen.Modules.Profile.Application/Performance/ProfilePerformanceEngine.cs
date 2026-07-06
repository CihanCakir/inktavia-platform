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
/// Calculates 18-metric performance scores across 5 dimensions for any profile type.
///
/// Cross-schema reads are performed via the ProfileDbContext ADO.NET connection —
/// same PostgreSQL instance, no HTTP calls, no module project dependencies.
///
/// Phase 19 rules enforced here:
/// - No automatic punishment, blocking, commission changes, or payout holds.
/// - No scoring in BFF — BFF is proxy-only.
/// - Cold-start: SampleSize &lt; 5 → neutral baseline (50), low confidence.
/// - Score history / decision logs are written by the calling CQRS handler, not here.
/// - Risk signal flag override (Flagged tier) is applied by the calling handler.
/// </summary>
[DocumentationInfo("ProfilePerformanceEngine",
    "Calculates 18-metric performance score for Provider/Participant/Owner profiles. " +
    "Reads from servicerequest, cargodry, payment, and profile schemas via raw SQL. " +
    "Returns ProfileScoreCalculationResult — never mutates entities directly.")]
public sealed class ProfilePerformanceEngine : IProfilePerformanceEngine
{
    private readonly ProfileDbContext _db;

    // ── Formula constants ────────────────────────────────────────────────────
    private const int     ColdStartThreshold  = 5;
    private const int     FullSampleSize       = 20;
    private const string  CalculationVersion  = "v1";
    private const decimal NeutralScore         = 50m;

    // ── Status constants (int values stored in DB) ───────────────────────────
    private const int SrStatusCompleted        = 41;   // ServiceRequestStatus.Completed
    private const int SrDisputeStatusResolved  = 6;    // ServiceRequestDisputeStatus.Resolved
    private const int PayoutStatusFailed       = 4;    // PayoutStatus.Failed
    private const int TxStatusDisputed         = 7;    // PaymentTransactionStatus.Disputed
    private const int KitStatusActivated       = 2;    // CargoDryKitStatus.Activated
    private const int KitStatusExpired         = 3;    // CargoDryKitStatus.Expired
    private const int KitStatusRenewed         = 4;    // CargoDryKitStatus.Renewed

    public ProfilePerformanceEngine(ProfileDbContext db)
    {
        _db = db;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public entry point
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<ProfileScoreCalculationResult> CalculateAsync(
        long            profileId,
        ProfileType     profileType,
        CancellationToken ct = default)
    {
        // Open a single shared ADO.NET connection for all cross-schema reads
        var conn = _db.Database.GetDbConnection();
        if (conn.State == ConnectionState.Closed)
            await conn.OpenAsync(ct);

        // ── Gather raw metric inputs per dimension ─────────────────────────
        var srMetrics = await GetServiceRequestMetricsAsync(conn, profileId, profileType, ct);
        var cdMetrics = await GetCargoDryMetricsAsync(conn, profileId, profileType, ct);
        var odMetrics = await GetOperationalDisciplineMetricsAsync(conn, profileId, profileType, ct);
        var frMetrics = await GetFinancialReliabilityMetricsAsync(conn, profileId, profileType, ct);
        var pcMetrics = await GetPlatformComplianceMetricsAsync(conn, profileId, profileType, ct);

        // ── Sample size = completed SR assignments (past 12 months) ─────────
        int sampleSize = srMetrics.TotalAssignments;

        // ── Cold-start rule ──────────────────────────────────────────────────
        if (sampleSize < ColdStartThreshold)
            return BuildColdStartResult(profileId, profileType, sampleSize);

        // ── Compute dimension scores (0–100 each) ────────────────────────────
        decimal srScore = ComputeServiceRequestScore(srMetrics);
        decimal cdScore = ComputeCargoDryScore(cdMetrics);
        decimal odScore = ComputeOperationalDisciplineScore(odMetrics);
        decimal frScore = ComputeFinancialReliabilityScore(frMetrics);
        decimal pcScore = ComputePlatformComplianceScore(pcMetrics);

        // RiskPenalty: applied by handler from active risk signals; engine returns 0 here.
        const decimal riskPenalty = 0m;

        // ── Overall score formula ─────────────────────────────────────────────
        decimal overall = Math.Clamp(
            srScore * 0.35m
          + cdScore * 0.25m
          + odScore * 0.15m
          + frScore * 0.15m
          + pcScore * 0.10m
          - riskPenalty,
            0m, 100m);

        // ── Confidence ────────────────────────────────────────────────────────
        decimal confidence     = Math.Min(1.0m, sampleSize / (decimal)FullSampleSize);
        var     confidenceLevel = ClassifyConfidence(confidence);

        // ── Priority tier ─────────────────────────────────────────────────────
        var tier = ClassifyTier(overall, confidence);

        // ── Components ────────────────────────────────────────────────────────
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
            ["profileType"]        = profileType.ToString(),
            ["sampleSize"]         = sampleSize,
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
            MetadataJson               = JsonSerializer.Serialize(metadata),
            CalculatedAtUtc            = now,
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Cold-start baseline
    // ─────────────────────────────────────────────────────────────────────────

    private static ProfileScoreCalculationResult BuildColdStartResult(
        long profileId, ProfileType profileType, int sampleSize)
    {
        var now = DateTime.UtcNow;

        var components = Enum.GetValues<PerformanceScoreCategory>()
            .Select(cat => MakeComponent(cat, NeutralScore, WeightFor(cat), now, null))
            .ToList();

        var metadata = new Dictionary<string, object?>
        {
            ["coldStart"]          = (object?)true,
            ["calculationVersion"] = CalculationVersion,
            ["profileType"]        = profileType.ToString(),
            ["sampleSize"]         = sampleSize,
        };

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
            MetadataJson               = JsonSerializer.Serialize(metadata),
            CalculatedAtUtc            = now,
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Cross-schema raw SQL readers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Metrics 1–6: ServiceRequest dimension.
    /// Only meaningful for Provider profiles.
    /// </summary>
    private async Task<SrMetrics> GetServiceRequestMetricsAsync(
        DbConnection conn, long profileId, ProfileType profileType, CancellationToken ct)
    {
        var m     = new SrMetrics();
        if (profileType != ProfileType.Provider) return m;

        try
        {
            var since = DateTime.UtcNow.AddMonths(-12);

            // Metrics 1, 2, 3: totals and response speed
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
                    m.TotalAssignments    = ToInt(r["TotalAssignments"]);
                    m.CompletedAssignments = ToInt(r["CompletedAssignments"]);
                    m.ResponsesUnder2h    = ToInt(r["ResponsesUnder2h"]);
                    m.ResponsesUnder6h    = ToInt(r["ResponsesUnder6h"]);
                }
            }

            // Metric 4: Client satisfaction (completions with ClientRating)
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
                    m.RatedCompletions    = ToInt(r["RatedCompletions"]);
                    m.HighRatedCompletions = ToInt(r["HighRatedCompletions"]);
                }
            }

            // Metrics 5+6: Disputes
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
        catch
        {
            // Graceful degradation — partial or empty data returns neutral scores
        }

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

    /// <summary>
    /// Metrics 7–9: CargoDry dimension.
    /// Only meaningful for Provider profiles.
    /// </summary>
    private async Task<CdMetrics> GetCargoDryMetricsAsync(
        DbConnection conn, long profileId, ProfileType profileType, CancellationToken ct)
    {
        var m = new CdMetrics();
        if (profileType != ProfileType.Provider) return m;

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
                m.AssignedKits      = ToInt(r["AssignedKits"]);
                m.ActivatedKits     = ToInt(r["ActivatedKits"]);
                m.KitsWithUsage     = ToInt(r["KitsWithUsage"]);
                m.RenewedKits       = ToInt(r["RenewedKits"]);
                m.EligibleForRenewal = ToInt(r["EligibleForRenewal"]);
            }
        }
        catch
        {
            // Graceful degradation
        }

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

    /// <summary>
    /// Metrics 10–12: OperationalDiscipline dimension.
    /// Only meaningful for Provider profiles.
    /// </summary>
    private async Task<OdMetrics> GetOperationalDisciplineMetricsAsync(
        DbConnection conn, long profileId, ProfileType profileType, CancellationToken ct)
    {
        var m = new OdMetrics();
        if (profileType != ProfileType.Provider) return m;

        try
        {
            var since = DateTime.UtcNow.AddMonths(-12);

            // Metric 10: On-time start (actual start ≤ scheduled start + 2h)
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
                    m.TotalAssignments    = ToInt(r["TotalAssignments"]);
                    m.TotalScheduledStarts = ToInt(r["TotalScheduledStarts"]);
                    m.OnTimeStarts        = ToInt(r["OnTimeStarts"]);
                }
            }

            // Metric 11: Work phase adherence
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

            // Metric 12: Document upload rate (completions exist = documents submitted)
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
        catch
        {
            // Graceful degradation
        }

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

    /// <summary>
    /// Metrics 13–15: FinancialReliability dimension.
    /// Only meaningful for Provider profiles.
    /// </summary>
    private async Task<FrMetrics> GetFinancialReliabilityMetricsAsync(
        DbConnection conn, long profileId, ProfileType profileType, CancellationToken ct)
    {
        var m = new FrMetrics();
        if (profileType != ProfileType.Provider) return m;

        try
        {
            var since = DateTime.UtcNow.AddMonths(-12);

            // Metric 13: Payout failure rate
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

            // Metric 14: Invoice issue rate (payout records with a linked invoice)
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

            // Metric 15: Commission conflict rate (disputed transactions)
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
        catch
        {
            // Graceful degradation
        }

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
    /// Metrics 16–18: PlatformCompliance dimension.
    /// Reads from profile.profile_metric_caches (populated by admin or external triggers).
    /// </summary>
    private async Task<PcMetrics> GetPlatformComplianceMetricsAsync(
        DbConnection conn, long profileId, ProfileType profileType, CancellationToken ct)
    {
        // Neutral defaults until cache is populated
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
        catch
        {
            // Graceful degradation
        }

        m.MetricsJson = Serialize(new
        {
            profileCompletionScore = m.ProfileCompletionScore,
            isIdVerified           = m.IsIdVerified,
            tosViolationCount      = m.TosViolationCount,
        });
        return m;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Score computation — per dimension
    // ─────────────────────────────────────────────────────────────────────────

    private static decimal ComputeServiceRequestScore(SrMetrics m)
    {
        decimal completionRate  = Rate(m.CompletedAssignments, m.TotalAssignments);
        decimal responseScore   = Rate(m.ResponsesUnder2h, m.TotalAssignments) * 0.60m
                                + Rate(m.ResponsesUnder6h, m.TotalAssignments) * 0.40m;
        decimal satisfactionRate = Rate(m.HighRatedCompletions, m.RatedCompletions);
        decimal disputeRate     = Rate(m.TotalDisputes, m.CompletedAssignments);
        decimal disputeScore    = Math.Max(0m, 100m - disputeRate * 2m);  // 50% dispute rate = 0
        decimal winRate         = Rate(m.WonDisputes, m.TotalDisputes);

        return Math.Clamp(
              completionRate  * 0.30m
            + responseScore   * 0.20m
            + satisfactionRate * 0.25m
            + disputeScore    * 0.15m
            + winRate         * 0.10m,
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
        decimal onTimeRate      = Rate(m.OnTimeStarts,     m.TotalScheduledStarts);
        decimal phaseAdherence  = Rate(m.CompletedPhases,  m.TotalPhases);
        decimal docUploadRate   = Rate(m.SrsWithDocuments, m.TotalAssignments);

        return Math.Clamp(
              onTimeRate     * 0.35m
            + phaseAdherence * 0.40m
            + docUploadRate  * 0.25m,
            0m, 100m);
    }

    private static decimal ComputeFinancialReliabilityScore(FrMetrics m)
    {
        decimal payoutFailRate = Rate(m.FailedPayouts,       m.TotalPayouts);
        decimal payoutScore    = Math.Max(0m, 100m - payoutFailRate * 3m);  // 33% fail = 0
        decimal invoiceRate    = Rate(m.IssuedInvoices,      m.TotalInvoiceableEvents);
        decimal conflictRate   = Rate(m.CommissionConflicts, m.TotalTransactions);
        decimal conflictScore  = Math.Max(0m, 100m - conflictRate * 5m);    // 20% conflict = 0

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
    // Classification helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Derives tier from overall score + confidence. Flagged is NOT set here —
    /// the calling handler applies Flagged when active risk signals are present.
    /// </summary>
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

    /// <summary>
    /// Safe percentage: returns 50 (neutral) when denominator is 0.
    /// This prevents cold-path data gaps from artificially punishing providers.
    /// </summary>
    private static decimal Rate(int numerator, int denominator)
        => denominator <= 0 ? NeutralScore : Math.Clamp(numerator * 100m / denominator, 0m, 100m);

    private static decimal WeightFor(PerformanceScoreCategory cat) => cat switch
    {
        PerformanceScoreCategory.ServiceRequest        => 0.35m,
        PerformanceScoreCategory.CargoDry              => 0.25m,
        PerformanceScoreCategory.OperationalDiscipline => 0.15m,
        PerformanceScoreCategory.FinancialReliability  => 0.15m,
        PerformanceScoreCategory.PlatformCompliance    => 0.10m,
        _                                              => 0.00m,
    };

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
            SnapshotId           = 0,    // Filled in by upsert handler after snapshot is saved
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
    // Private metric bags
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
}
