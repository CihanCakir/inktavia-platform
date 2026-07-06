using Aizen.Bff.AdminPanel.Application.AdminProfilePerformance.Command.RaiseProfileRiskSignal;
using Aizen.Bff.AdminPanel.Application.AdminProfilePerformance.Command.RecalculateProfilePerformance;
using Aizen.Bff.AdminPanel.Application.AdminProfilePerformance.Command.ResolveProfileRiskSignal;
using Aizen.Bff.AdminPanel.Application.AdminProfilePerformance.Dto;
using Aizen.Bff.AdminPanel.Application.AdminProfilePerformance.Query.GetProfilePerformanceComponents;
using Aizen.Bff.AdminPanel.Application.AdminProfilePerformance.Query.GetProfilePerformanceDecisionLogs;
using Aizen.Bff.AdminPanel.Application.AdminProfilePerformance.Query.GetProfilePerformanceHistory;
using Aizen.Bff.AdminPanel.Application.AdminProfilePerformance.Query.GetProfilePerformanceRiskSignals;
using Aizen.Bff.AdminPanel.Application.AdminProfilePerformance.Query.GetProfilePerformanceSnapshot;
using Aizen.Bff.AdminPanel.Application.AdminProfilePerformance.Query.GetProfilePerformanceSnapshotsByTier;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

/// <summary>
/// AdminPanel BFF — Profile Performance endpoints.
///
/// All endpoints proxy to Aizen.Modules.Profile at /api/v1/profile/admin/performance/*.
/// Phase 20 rule: BFF is proxy-only — no score calculation occurs here.
/// Phase 20 rule: All endpoints require AdminPanelAccess policy.
/// </summary>
[ApiController]
[Route("api/v1/admin-panel/profile/performance")]
[Tags("Admin Panel - Profile Performance")]
[Authorize(Policy = "AdminPanelAccess")]
public sealed class AdminProfilePerformanceController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public AdminProfilePerformanceController(
        IHttpContextAccessor httpContextAccessor,
        IAizenCQRSProcessor  cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    // ─── Snapshot ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the current performance snapshot + 5 score components for a provider.
    /// Found=false when no snapshot exists yet (pre-calculation or cold-start).
    /// </summary>
    [HttpGet("{profileId:long}/{profileType}")]
    [ProducesResponseType(typeof(ProfilePerformanceSnapshotWithComponentsBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProfilePerformanceSnapshotWithComponentsBffDto>> GetSnapshot(
        long   profileId,
        string profileType,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetProfilePerformanceSnapshotBffQuery
        {
            ProfileId   = profileId,
            ProfileType = profileType,
        }, ct);
        return SetResponse(result?.Data);
    }

    // ─── Tier listing ─────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a paged list of provider performance snapshots filtered by tier.
    /// Tiers: ColdStart, Bronze, Silver, Gold, Platinum, Flagged.
    /// </summary>
    [HttpGet("tier/{tier}")]
    [ProducesResponseType(typeof(ProfileSnapshotPagedBffResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProfileSnapshotPagedBffResultDto>> GetSnapshotsByTier(
        string tier,
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetProfilePerformanceSnapshotsByTierBffQuery
        {
            Tier     = tier,
            Page     = page,
            PageSize = pageSize,
        }, ct);
        return SetResponse(result?.Data);
    }

    // ─── Score history ────────────────────────────────────────────────────────

    /// <summary>
    /// Returns paged score recalculation history for a profile (append-only log).
    /// </summary>
    [HttpGet("{profileId:long}/{profileType}/history")]
    [ProducesResponseType(typeof(ProfileScoreHistoryPagedBffResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProfileScoreHistoryPagedBffResultDto>> GetScoreHistory(
        long   profileId,
        string profileType,
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetProfilePerformanceHistoryBffQuery
        {
            ProfileId   = profileId,
            ProfileType = profileType,
            Page        = page,
            PageSize    = pageSize,
        }, ct);
        return SetResponse(result?.Data);
    }

    // ─── Decision logs ────────────────────────────────────────────────────────

    /// <summary>
    /// Returns paged decision audit log for a profile.
    /// Events: TierChanged, RiskSignalRaised, RiskSignalResolved, ColdStartBaseline.
    /// </summary>
    [HttpGet("{profileId:long}/{profileType}/decision-logs")]
    [ProducesResponseType(typeof(ProfileDecisionLogPagedBffResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProfileDecisionLogPagedBffResultDto>> GetDecisionLogs(
        long   profileId,
        string profileType,
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetProfilePerformanceDecisionLogsBffQuery
        {
            ProfileId   = profileId,
            ProfileType = profileType,
            Page        = page,
            PageSize    = pageSize,
        }, ct);
        return SetResponse(result?.Data);
    }

    // ─── Risk signals ─────────────────────────────────────────────────────────

    /// <summary>
    /// Returns paged risk signals for a profile.
    /// Pass activeOnly=true to return only unresolved signals.
    /// </summary>
    [HttpGet("{profileId:long}/{profileType}/risk-signals")]
    [ProducesResponseType(typeof(ProfileRiskSignalPagedBffResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProfileRiskSignalPagedBffResultDto>> GetRiskSignals(
        long   profileId,
        string profileType,
        [FromQuery] bool? activeOnly = null,
        [FromQuery] int   page       = 1,
        [FromQuery] int   pageSize   = 25,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetProfilePerformanceRiskSignalsBffQuery
        {
            ProfileId   = profileId,
            ProfileType = profileType,
            ActiveOnly  = activeOnly,
            Page        = page,
            PageSize    = pageSize,
        }, ct);
        return SetResponse(result?.Data);
    }

    // ─── Score components ─────────────────────────────────────────────────────

    /// <summary>
    /// Returns all 5 per-dimension score components for the current snapshot.
    /// </summary>
    [HttpGet("{profileId:long}/{profileType}/components")]
    [ProducesResponseType(typeof(List<ProfileScoreComponentBffDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<ProfileScoreComponentBffDto>>> GetScoreComponents(
        long   profileId,
        string profileType,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetProfilePerformanceComponentsBffQuery
        {
            ProfileId   = profileId,
            ProfileType = profileType,
        }, ct);
        return SetResponse(result?.Data);
    }

    // ─── Commands ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Triggers a full score recalculation for the given provider profile.
    /// Phase 20 rule: no score calculation in BFF — proxies to Profile module.
    /// </summary>
    [HttpPost("{profileId:long}/{profileType}/recalculate")]
    [ProducesResponseType(typeof(RecalculateProfilePerformanceBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RecalculateProfilePerformanceBffResult>> Recalculate(
        long   profileId,
        string profileType,
        [FromQuery] string? reason = null,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new RecalculateProfilePerformanceBffCommand
        {
            ProfileId   = profileId,
            ProfileType = profileType,
            Reason      = reason,
        }, ct);
        return SetResponse(result?.Result);
    }

    /// <summary>
    /// Raises a risk signal for a provider profile.
    /// High/Critical signals immediately override the tier to Flagged in the module layer.
    /// Phase 20 rule: no tier logic in BFF.
    /// </summary>
    [HttpPost("{profileId:long}/{profileType}/risk-signals")]
    [ProducesResponseType(typeof(ProfileRiskSignalBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProfileRiskSignalBffDto>> RaiseRiskSignal(
        long   profileId,
        string profileType,
        [FromBody] RaiseProfileRiskSignalBffRequest body,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new RaiseProfileRiskSignalBffCommand
        {
            ProfileId   = profileId,
            ProfileType = profileType,
            Body        = body,
        }, ct);
        return SetResponse(result?.Result);
    }

    /// <summary>
    /// Resolves an active risk signal by id.
    /// If no High/Critical signals remain after resolution, the module layer restores the tier.
    /// Phase 20 rule: no tier logic in BFF.
    /// </summary>
    [HttpPost("risk-signals/{signalId:long}/resolve")]
    [ProducesResponseType(typeof(ProfileRiskSignalBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProfileRiskSignalBffDto>> ResolveRiskSignal(
        long signalId,
        [FromBody] ResolveProfileRiskSignalBffRequest body,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new ResolveProfileRiskSignalBffCommand
        {
            SignalId = signalId,
            Body     = body,
        }, ct);
        return SetResponse(result?.Result);
    }
}
