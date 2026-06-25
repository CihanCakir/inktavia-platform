using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Core.RemoteCall.Abstraction;
using Refit;

namespace Aizen.Bff.AdminPanel.Application.Common.RemoteClients;

[DocumentationInfo("CargoDry admin BFF remote call", "Defines synchronous BFF-to-CargoDry calls for admin kit management and the public onboarding flow.")]
public interface IAdminCargoDryBffRemoteCall : IAizenRemoteCall
{
    // ── Admin Kit Management ─────────────────────────────────────────────────

    [Get("/api/v1/cargodry/admin/kits")]
    Task<CargoDryKitListBffDto> GetKitsAsync(
        [Query] string? status   = null,
        [Query] string? search   = null,
        [Query] long?   vesselId = null,
        [Query] int     page     = 1,
        [Query] int     pageSize = 25,
        CancellationToken ct = default);

    [Get("/api/v1/cargodry/admin/stats")]
    Task<CargoDryStatsBffDto> GetStatsAsync(CancellationToken ct = default);

    [Post("/api/v1/cargodry/admin/batches/generate")]
    Task<GenerateBatchBffResultDto> GenerateBatchAsync(
        [Body] GenerateBatchBffRequest request,
        CancellationToken ct = default);

    [Post("/api/v1/cargodry/admin/kits/{id}/revoke")]
    Task<bool> RevokeKitAsync(
        long id,
        [Body] RevokeKitBffRequest request,
        CancellationToken ct = default);

    [Post("/api/v1/cargodry/admin/kits/{id}/extend")]
    Task<CargoDryKitBffDto> ExtendKitAsync(
        long id,
        [Body] ExtendKitBffRequest request,
        CancellationToken ct = default);

    // ── Onboarding (Public + User) ────────────────────────────────────────────

    // ── Reporting & Analytics ─────────────────────────────────────────────────

    [Get("/api/v1/cargodry/admin/reports/usage")]
    Task<CargoDryKitUsageReportBffDto> GetUsageReportAsync(
        [Query] DateTimeOffset? dateFrom = null,
        [Query] DateTimeOffset? dateTo   = null,
        CancellationToken ct = default);

    [Get("/api/v1/cargodry/admin/reports/usage/export")]
    Task<HttpResponseMessage> ExportUsageReportAsync(
        [Query] string format,
        [Query] DateTimeOffset? dateFrom = null,
        [Query] DateTimeOffset? dateTo   = null,
        CancellationToken ct = default);

    [Get("/api/v1/cargodry/admin/analytics")]
    Task<CargoDryAnalyticsBffDto> GetAnalyticsAsync(CancellationToken ct = default);

    // ── Onboarding (Public + User) ────────────────────────────────────────────

    [Post("/api/v1/cargodry/public/validate")]
    Task<CargoDryValidationBffDto> ValidateKitAsync(
        [Body] ValidateKitBffRequest request,
        CancellationToken ct = default);

    [Post("/api/v1/cargodry/kits/activate")]
    Task<CargoDryKitBffDto> ActivateKitAsync(
        [Body] ActivateKitBffRequest request,
        CancellationToken ct = default);
}

// ── Request DTOs ─────────────────────────────────────────────────────────────

public sealed class GenerateBatchBffRequest
{
    public string ProductCode { get; init; } = default!;
    public int    Count       { get; init; }
}

public sealed class RevokeKitBffRequest
{
    public string Reason { get; init; } = default!;
}

public sealed class ExtendKitBffRequest
{
    public int AddedDays { get; init; }
}

public sealed class ValidateKitBffRequest
{
    public string  SerialNumber { get; init; } = default!;
    public string  BatchCode    { get; init; } = default!;
    public string? Signature    { get; init; }
}

public sealed class ActivateKitBffRequest
{
    public string ActivationToken { get; init; } = default!;
    public long   VesselId        { get; init; }
}
