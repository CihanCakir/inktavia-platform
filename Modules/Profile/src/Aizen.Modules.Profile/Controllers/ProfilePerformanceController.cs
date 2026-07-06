using Aizen.Modules.Profile.Abstraction.Dtos.Performance;
using Aizen.Modules.Profile.Abstraction.Enums.Performance;
using Aizen.Modules.Profile.Application.Commands.Performance.RaiseRiskSignal;
using Aizen.Modules.Profile.Application.Commands.Performance.ResolveRiskSignal;
using Aizen.Modules.Profile.Application.Commands.Performance.UpsertPerformanceSnapshot;
using Aizen.Modules.Profile.Application.Queries.Performance.GetDecisionLogsByProfile;
using Aizen.Modules.Profile.Application.Queries.Performance.GetPerformanceSnapshotByProfile;
using Aizen.Modules.Profile.Application.Queries.Performance.GetPerformanceSnapshotsByTier;
using Aizen.Modules.Profile.Application.Queries.Performance.GetProfilePriorityPreview;
using Aizen.Modules.Profile.Application.Queries.Performance.GetRiskSignalsByProfile;
using Aizen.Modules.Profile.Application.Queries.Performance.GetScoreComponentsByProfile;
using Aizen.Modules.Profile.Application.Queries.Performance.GetScoreHistoryByProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Profile.Controllers;

[ApiController]
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/v1/profile/admin/performance")]
public sealed class ProfilePerformanceController : ControllerBase
{
    private readonly ISender _sender;

    public ProfilePerformanceController(ISender sender)
    {
        _sender = sender;
    }

    // ─── Snapshot Queries ────────────────────────────────────────────────────

    /// <summary>
    /// Returns the current performance snapshot and score components for a single profile.
    /// Found=false when no snapshot exists yet (pre-calculation).
    /// </summary>
    [HttpGet("{profileId:long}/{profileType}")]
    public async Task<IActionResult> GetSnapshot(
        long        profileId,
        ProfileType profileType,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetPerformanceSnapshotByProfileQuery
        {
            ProfileId   = profileId,
            ProfileType = profileType,
        }, ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns a paged list of snapshots filtered by priority tier.
    /// Used for tier-based provider discovery and admin dashboards.
    /// </summary>
    [HttpGet("tier/{tier}")]
    public async Task<IActionResult> GetByTier(
        PriorityTier tier,
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetPerformanceSnapshotsByTierQuery
        {
            Tier     = tier,
            Page     = page,
            PageSize = pageSize,
        }, ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns paged score recalculation history for a profile (append-only).
    /// </summary>
    [HttpGet("{profileId:long}/{profileType}/history")]
    public async Task<IActionResult> GetScoreHistory(
        long        profileId,
        ProfileType profileType,
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetScoreHistoryByProfileQuery
        {
            ProfileId   = profileId,
            ProfileType = profileType,
            Page        = page,
            PageSize    = pageSize,
        }, ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns paged decision audit log for a profile (append-only).
    /// Includes TierChanged, RiskSignalRaised, RiskSignalResolved, ColdStartBaseline events.
    /// </summary>
    [HttpGet("{profileId:long}/{profileType}/decision-logs")]
    public async Task<IActionResult> GetDecisionLogs(
        long        profileId,
        ProfileType profileType,
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetDecisionLogsByProfileQuery
        {
            ProfileId   = profileId,
            ProfileType = profileType,
            Page        = page,
            PageSize    = pageSize,
        }, ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns paged risk signals for a profile.
    /// Pass activeOnly=true to return only unresolved signals.
    /// </summary>
    [HttpGet("{profileId:long}/{profileType}/risk-signals")]
    public async Task<IActionResult> GetRiskSignals(
        long        profileId,
        ProfileType profileType,
        [FromQuery] bool? activeOnly = null,
        [FromQuery] int   page       = 1,
        [FromQuery] int   pageSize   = 25,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetRiskSignalsByProfileQuery
        {
            ProfileId   = profileId,
            ProfileType = profileType,
            ActiveOnly  = activeOnly,
            Page        = page,
            PageSize    = pageSize,
        }, ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns all 5 per-dimension score components for the current snapshot.
    /// </summary>
    [HttpGet("{profileId:long}/{profileType}/components")]
    public async Task<IActionResult> GetScoreComponents(
        long        profileId,
        ProfileType profileType,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetScoreComponentsByProfileQuery
        {
            ProfileId   = profileId,
            ProfileType = profileType,
        }, ct);
        return Ok(result);
    }

    // ─── Phase 21 — Priority Preview ─────────────────────────────────────────

    /// <summary>
    /// Phase 21 — Read-only priority preview for admin decision support.
    ///
    /// Returns a ranked list of candidate providers with priority scores and explanation factors.
    /// Supported contexts (MVP): ServiceRequestProviderRecommendation, AdminAssignmentSuggestion.
    ///
    /// Hard rules:
    /// - Does NOT change assignment logic.
    /// - Does NOT change provider search ranking.
    /// - Does NOT create penalties or modify any entity.
    /// - All scores come from the existing snapshot — no recalculation.
    /// </summary>
    [HttpPost("priority-preview")]
    public async Task<IActionResult> GetPriorityPreview(
        [FromBody] ProfilePriorityPreviewRequestDto request,
        CancellationToken ct = default)
    {
        var actorUserId = User.FindFirst("sub")?.Value;

        var result = await _sender.Send(new GetProfilePriorityPreviewQuery
        {
            CandidateProfileIds = request.CandidateProfileIds,
            Context             = request.Context,
            CategoryCode        = request.CategoryCode,
            LocationCode        = request.LocationCode,
            MaxResults          = request.MaxResults,
            LogDecision         = request.LogDecision,
            ActorUserId         = actorUserId,
        }, ct);

        return Ok(result);
    }

    // ─── Commands ────────────────────────────────────────────────────────────

    /// <summary>
    /// Triggers a full score recalculation for the given profile.
    /// Creates or updates the snapshot, replaces score components,
    /// appends score history and a decision log entry.
    ///
    /// Phase 19 rule: no automatic punishment, blocking, or commission changes.
    /// </summary>
    [HttpPost("{profileId:long}/{profileType}/recalculate")]
    public async Task<IActionResult> Recalculate(
        long        profileId,
        ProfileType profileType,
        [FromQuery] string? reason = null,
        CancellationToken ct = default)
    {
        var actorUserId = User.FindFirst("sub")?.Value;
        var result = await _sender.Send(new UpsertPerformanceSnapshotCommand
        {
            ProfileId     = profileId,
            ProfileType   = profileType,
            TriggerReason = reason ?? "AdminForced",
            ActorUserId   = actorUserId,
        }, ct);
        return Ok(result);
    }

    /// <summary>
    /// Raises a risk signal for a profile.
    /// High or Critical signals immediately override the snapshot tier to Flagged.
    ///
    /// Phase 19 rule: no automatic commission changes, payout holds, or blocking.
    /// </summary>
    [HttpPost("{profileId:long}/{profileType}/risk-signals")]
    public async Task<IActionResult> RaiseRiskSignal(
        long        profileId,
        ProfileType profileType,
        [FromBody] RaiseRiskSignalRequest request,
        CancellationToken ct = default)
    {
        var actorUserId = User.FindFirst("sub")?.Value;
        var result = await _sender.Send(new RaiseRiskSignalCommand
        {
            ProfileId      = profileId,
            ProfileType    = profileType,
            Severity       = request.Severity,
            SignalCode     = request.SignalCode,
            Description    = request.Description,
            SourceModule   = request.SourceModule,
            SourceEntityId = request.SourceEntityId,
            ActorUserId    = actorUserId,
        }, ct);
        return Ok(result);
    }

    /// <summary>
    /// Resolves an active risk signal by id.
    /// If no High/Critical signals remain after resolution, clears the Flagged tier
    /// override and restores the tier from the current snapshot score.
    /// Appends a RiskSignalResolved decision log entry.
    /// </summary>
    [HttpPost("risk-signals/{signalId:long}/resolve")]
    public async Task<IActionResult> ResolveRiskSignal(
        long signalId,
        [FromBody] ResolveRiskSignalRequest request,
        CancellationToken ct = default)
    {
        var actorUserId      = User.FindFirst("sub")?.Value;
        var resolvedByUserId = long.TryParse(User.FindFirst("userId")?.Value, out var uid) ? uid : (long?)null;

        var result = await _sender.Send(new ResolveRiskSignalCommand
        {
            SignalId         = signalId,
            ResolutionNote   = request.ResolutionNote,
            ResolvedByUserId = resolvedByUserId,
            ActorUserId      = actorUserId,
        }, ct);
        return Ok(result);
    }
}

// ─── Request Models ───────────────────────────────────────────────────────────

public sealed record RaiseRiskSignalRequest
{
    public RiskSignalSeverity Severity       { get; init; }
    public string             SignalCode     { get; init; } = default!;
    public string             Description    { get; init; } = default!;
    public string?            SourceModule   { get; init; }
    public long?              SourceEntityId { get; init; }
}

public sealed record ResolveRiskSignalRequest
{
    public string? ResolutionNote { get; init; }
}
