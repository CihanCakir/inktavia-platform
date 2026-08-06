using Aizen.Bff.AdminPanel.Application.AdminProfilePerformance.Dto;
using Aizen.Core.RemoteCall.Abstraction;
using Refit;

namespace Aizen.Bff.AdminPanel.Application.Common.RemoteClients;

[DocumentationInfo("Admin profile performance BFF remote call",
    "Proxy interface from AdminPanel BFF to Aizen.Modules.Profile — performance sub-module. " +
    "Covers snapshot reads, tier listing, score history, decision logs, risk signals, " +
    "score components, admin mutation endpoints (recalculate, raise/resolve risk signal), " +
    "and Phase 21 priority preview. " +
    "Auth headers (Authorization service token + optional X-Aizen-Bff-Assertion) are injected automatically " +
    "by AdminPanelBffAuthDelegatingHandler. " +
    "Phase 20/21 rule: BFF is proxy-only — no score calculation occurs here.")]
public interface IProfilePerformanceRemoteCall : IAizenRemoteCall
{
    // ─── Snapshot ─────────────────────────────────────────────────────────────

    [AizenRemoteCallGet("/api/v1/profile/admin/performance/{profileId}/{profileType}")]
    Task<ProfilePerformanceSnapshotWithComponentsBffDto> GetSnapshotAsync(
        long   profileId,
        string profileType,
        CancellationToken ct = default);

    // ─── Tier listing ─────────────────────────────────────────────────────────

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

    // ─── Phase 21 — Priority Preview ─────────────────────────────────────────

    /// <summary>
    /// Phase 21 — Read-only admin priority preview.
    /// POST /api/v1/profile/admin/performance/priority-preview
    /// Phase 21 rule: proxy-only — no score calculation in BFF.
    /// </summary>
    [AizenRemoteCallPost("/api/v1/profile/admin/performance/priority-preview")]
    Task<ProfilePriorityPreviewBffResult> GetProfilePriorityPreviewAsync(
        [AizenRemoteCallBody] ProfilePriorityPreviewBffRequest body,
        CancellationToken ct = default);
}
