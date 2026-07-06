using Aizen.Bff.AdminPanel.Application.AdminProfilePerformance.Dto;
using Aizen.Core.RemoteCall.Abstraction;
using Refit;

namespace Aizen.Bff.AdminPanel.Application.Common.RemoteClients;

[DocumentationInfo("Admin profile performance BFF remote call",
    "Proxy interface from AdminPanel BFF to Aizen.Modules.Profile — performance sub-module. " +
    "Covers snapshot reads, tier listing, score history, decision logs, risk signals, " +
    "score components, and admin mutation endpoints (recalculate, raise/resolve risk signal). " +
    "Auth headers (Authorization + X-Aizen-User-Token) are injected automatically " +
    "by AdminPanelBffAuthDelegatingHandler. " +
    "Phase 20 rule: BFF is proxy-only — no score calculation occurs here.")]
public interface IAdminProfilePerformanceBffRemoteCall : IAizenRemoteCall
{
    // ─── Snapshot ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the current performance snapshot + score components for a provider.
    /// Found=false when no snapshot exists yet.
    /// </summary>
    [AizenRemoteCallGet("/api/v1/profile/admin/performance/{profileId}/{profileType}")]
    Task<ProfilePerformanceSnapshotWithComponentsBffDto> GetSnapshotAsync(
        long   profileId,
        string profileType,
        CancellationToken ct = default);

    // ─── Tier listing ─────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a paged list of snapshots for the given priority tier.
    /// </summary>
    [AizenRemoteCallGet("/api/v1/profile/admin/performance/tier/{tier}")]
    Task<ProfileSnapshotPagedBffResultDto> GetSnapshotsByTierAsync(
        string      tier,
        [Query] int page     = 1,
        [Query] int pageSize = 25,
        CancellationToken ct = default);

    // ─── Score history ────────────────────────────────────────────────────────

    [AizenRemoteCallGet("/api/v1/profile/admin/performance/{profileId}/{profileType}/history")]
    Task<ProfileScoreHistoryPagedBffResultDto> GetScoreHistoryAsync(
        long        profileId,
        string      profileType,
        [Query] int page     = 1,
        [Query] int pageSize = 25,
        CancellationToken ct = default);

    // ─── Decision logs ────────────────────────────────────────────────────────

    [AizenRemoteCallGet("/api/v1/profile/admin/performance/{profileId}/{profileType}/decision-logs")]
    Task<ProfileDecisionLogPagedBffResultDto> GetDecisionLogsAsync(
        long        profileId,
        string      profileType,
        [Query] int page     = 1,
        [Query] int pageSize = 25,
        CancellationToken ct = default);

    // ─── Risk signals ─────────────────────────────────────────────────────────

    [AizenRemoteCallGet("/api/v1/profile/admin/performance/{profileId}/{profileType}/risk-signals")]
    Task<ProfileRiskSignalPagedBffResultDto> GetRiskSignalsAsync(
        long         profileId,
        string       profileType,
        [Query] bool? activeOnly = null,
        [Query] int   page       = 1,
        [Query] int   pageSize   = 25,
        CancellationToken ct = default);

    // ─── Score components ─────────────────────────────────────────────────────

    [AizenRemoteCallGet("/api/v1/profile/admin/performance/{profileId}/{profileType}/components")]
    Task<List<ProfileScoreComponentBffDto>> GetScoreComponentsAsync(
        long   profileId,
        string profileType,
        CancellationToken ct = default);

    // ─── Commands ─────────────────────────────────────────────────────────────

    [AizenRemoteCallPost("/api/v1/profile/admin/performance/{profileId}/{profileType}/recalculate")]
    Task<RecalculateProfilePerformanceBffResult> RecalculateAsync(
        long    profileId,
        string  profileType,
        [Query] string? reason = null,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/profile/admin/performance/{profileId}/{profileType}/risk-signals")]
    Task<ProfileRiskSignalBffDto> RaiseRiskSignalAsync(
        long        profileId,
        string      profileType,
        [AizenRemoteCallBody] RaiseProfileRiskSignalBffRequest body,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/profile/admin/performance/risk-signals/{signalId}/resolve")]
    Task<ProfileRiskSignalBffDto> ResolveRiskSignalAsync(
        long        signalId,
        [AizenRemoteCallBody] ResolveProfileRiskSignalBffRequest body,
        CancellationToken ct = default);
}
