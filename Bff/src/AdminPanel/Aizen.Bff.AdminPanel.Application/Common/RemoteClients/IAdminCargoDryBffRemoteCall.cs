using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Core.RemoteCall.Abstraction;
using Refit;

namespace Aizen.Bff.AdminPanel.Application.Common.RemoteClients;

[DocumentationInfo("CargoDry admin BFF remote call",
    "Defines BFF-to-CargoDry calls. Auth headers (Authorization + X-Aizen-User-Token) are " +
    "injected automatically by AdminPanelBffAuthDelegatingHandler. " +
    "Public onboarding endpoints have no auth requirement but still pass through the handler safely.")]
public interface IAdminCargoDryBffRemoteCall : IAizenRemoteCall
{
    // ── Admin Kit Management ──────────────────────────────────────────────────

    [AizenRemoteCallGet("/api/v1/cargodry/admin/kits")]
    Task<CargoDryKitListBffDto> GetKitsAsync(
        [Query] string? status,
        [Query] string? search,
        [Query] long?   vesselId,
        [Query] string? batchCode,
        [Query] int     page,
        [Query] int     pageSize,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/cargodry/admin/stats")]
    Task<CargoDryStatsBffDto> GetStatsAsync(
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/cargodry/admin/batches/generate")]
    Task<GenerateBatchBffResultDto> GenerateBatchAsync(
        [AizenRemoteCallBody] GenerateBatchBffRequest request,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/cargodry/admin/kits/{id}/revoke")]
    Task<RevokeKitBffResponse> RevokeKitAsync(
        long id,
        [AizenRemoteCallBody] RevokeKitBffRequest request,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/cargodry/admin/products")]
    Task<List<CargoDryProductBffDto>> GetProductsAsync(
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/cargodry/admin/batches")]
    Task<CargoDryBatchListBffDto> GetBatchesAsync(
        [Query] int page,
        [Query] int pageSize,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/cargodry/admin/batches/{batchCode}")]
    Task<CargoDryBatchBffDto> GetBatchByCodeAsync(
        string batchCode,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/cargodry/admin/kits/{id}/renew")]
    Task<CargoDryKitBffDto> RenewKitAsync(
        long id,
        [AizenRemoteCallBody] RenewKitBffRequest request,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/cargodry/admin/kits/{id}/extend")]
    Task<CargoDryKitBffDto> ExtendKitAsync(
        long id,
        [AizenRemoteCallBody] ExtendKitBffRequest request,
        CancellationToken ct = default);

    // ── Reporting & Analytics ─────────────────────────────────────────────────

    [AizenRemoteCallGet("/api/v1/cargodry/admin/reports/usage")]
    Task<CargoDryKitUsageReportBffDto> GetUsageReportAsync(
        [Query] DateTimeOffset? dateFrom,
        [Query] DateTimeOffset? dateTo,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/cargodry/admin/reports/usage/export")]
    Task<HttpResponseMessage> ExportUsageReportAsync(
        [Query] string          format,
        [Query] DateTimeOffset? dateFrom,
        [Query] DateTimeOffset? dateTo,
        CancellationToken ct = default);

    [AizenRemoteCallGet("/api/v1/cargodry/admin/analytics")]
    Task<CargoDryAnalyticsRawBffDto> GetAnalyticsAsync(
        CancellationToken ct = default);

    // ── Onboarding (Public — no auth headers required) ───────────────────────

    [AizenRemoteCallPost("/api/v1/cargodry/public/validate")]
    Task<CargoDryValidationBffDto> ValidateKitAsync(
        [AizenRemoteCallBody] ValidateKitBffRequest request,
        CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/cargodry/kits/activate")]
    Task<CargoDryKitBffDto> ActivateKitAsync(
        [AizenRemoteCallBody] ActivateKitBffRequest request,
        CancellationToken ct = default);
}
