using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryAnalytics;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryUsageReport;
using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

[ApiController]
[Route("api/v1/admin-panel/cargodry")]
[Tags("Admin Panel - CargoDry")]
[Authorize(Policy = "AdminPanelAccess")]
public sealed class AdminCargoDryController : AizenWebApiController
{
    private readonly IAdminCargoDryBffRemoteCall                _cargoDry;
    private readonly IAizenCQRSProcessor                        _cqrs;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public AdminCargoDryController(
        IHttpContextAccessor httpContextAccessor,
        IAdminCargoDryBffRemoteCall cargoDry,
        IAizenCQRSProcessor cqrs,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
        : base(httpContextAccessor)
    {
        _cargoDry             = cargoDry;
        _cqrs                 = cqrs;
        _serviceTokenProvider = serviceTokenProvider;
    }

    // ── Auth helpers ──────────────────────────────────────────────────────────

    private string GetUserToken()
    {
        var raw = HttpContext.Request.Headers.Authorization.ToString();
        return raw.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? raw["Bearer ".Length..]
            : raw;
    }

    private async Task<(string bearer, string userToken)> GetAuthAsync(CancellationToken ct)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(ct);
        return ($"Bearer {serviceToken}", GetUserToken());
    }

    /// <summary>GET /api/v1/admin-panel/cargodry/kits</summary>
    [HttpGet("kits")]
    [ProducesResponseType(typeof(CargoDryKitListBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryKitListBffDto>> GetKits(
        [FromQuery] string? status   = null,
        [FromQuery] string? search   = null,
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 25,
        CancellationToken ct = default)
    {
        var (bearer, userToken) = await GetAuthAsync(ct);
        var result = await _cargoDry.GetKitsAsync(status, search, null, page, pageSize, bearer, userToken, ct);
        return SetResponse(result);
    }

    /// <summary>GET /api/v1/admin-panel/cargodry/stats</summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(CargoDryStatsBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryStatsBffDto>> GetStats(CancellationToken ct)
    {
        var (bearer, userToken) = await GetAuthAsync(ct);
        var result = await _cargoDry.GetStatsAsync(bearer, userToken, ct);
        return SetResponse(result);
    }

    /// <summary>POST /api/v1/admin-panel/cargodry/batches/generate</summary>
    [HttpPost("batches/generate")]
    [ProducesResponseType(typeof(GenerateBatchBffResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GenerateBatchBffResultDto>> GenerateBatch(
        [FromBody] GenerateBatchBffRequest request, CancellationToken ct)
    {
        var (bearer, userToken) = await GetAuthAsync(ct);
        var result = await _cargoDry.GenerateBatchAsync(request, bearer, userToken, ct);
        return SetResponse(result);
    }

    /// <summary>POST /api/v1/admin-panel/cargodry/kits/{id}/revoke</summary>
    [HttpPost("kits/{id:long}/revoke")]
    [ProducesResponseType(typeof(RevokeKitBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RevokeKitBffResponse>> RevokeKit(
        long id, [FromBody] RevokeKitBffRequest request, CancellationToken ct)
    {
        var (bearer, userToken) = await GetAuthAsync(ct);
        var result = await _cargoDry.RevokeKitAsync(id, request, bearer, userToken, ct);
        return SetResponse(result);
    }

    /// <summary>GET /api/v1/admin-panel/cargodry/products</summary>
    [HttpGet("products")]
    [ProducesResponseType(typeof(List<CargoDryProductBffDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<CargoDryProductBffDto>>> GetProducts(CancellationToken ct)
    {
        var (bearer, userToken) = await GetAuthAsync(ct);
        var result = await _cargoDry.GetProductsAsync(bearer, userToken, ct);
        return SetResponse(result);
    }

    /// <summary>GET /api/v1/admin-panel/cargodry/batches</summary>
    [HttpGet("batches")]
    [ProducesResponseType(typeof(CargoDryBatchListBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryBatchListBffDto>> GetBatches(
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var (bearer, userToken) = await GetAuthAsync(ct);
        var result = await _cargoDry.GetBatchesAsync(page, pageSize, bearer, userToken, ct);
        return SetResponse(result);
    }

    /// <summary>POST /api/v1/admin-panel/cargodry/kits/{id}/renew</summary>
    [HttpPost("kits/{id:long}/renew")]
    [ProducesResponseType(typeof(CargoDryKitBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryKitBffDto>> RenewKit(
        long id, [FromBody] RenewKitBffRequest request, CancellationToken ct)
    {
        var (bearer, userToken) = await GetAuthAsync(ct);
        var result = await _cargoDry.RenewKitAsync(id, request, bearer, userToken, ct);
        return SetResponse(result);
    }

    /// <summary>POST /api/v1/admin-panel/cargodry/kits/{id}/extend</summary>
    [HttpPost("kits/{id:long}/extend")]
    [ProducesResponseType(typeof(CargoDryKitBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryKitBffDto>> ExtendKit(
        long id, [FromBody] ExtendKitBffRequest request, CancellationToken ct)
    {
        var (bearer, userToken) = await GetAuthAsync(ct);
        var result = await _cargoDry.ExtendKitAsync(id, request, bearer, userToken, ct);
        return SetResponse(result);
    }

    /// <summary>GET /api/v1/admin-panel/cargodry/reports/usage</summary>
    [HttpGet("reports/usage")]
    [ProducesResponseType(typeof(CargoDryKitUsageReportBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryKitUsageReportBffDto>> GetUsageReport(
        [FromQuery] DateTimeOffset? dateFrom = null,
        [FromQuery] DateTimeOffset? dateTo   = null,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetCargoDryUsageReportBffQuery { DateFrom = dateFrom, DateTo = dateTo }, ct);
        return SetResponse(result?.Report);
    }

    /// <summary>GET /api/v1/admin-panel/cargodry/reports/usage/export</summary>
    [HttpGet("reports/usage/export")]
    public async Task<IActionResult> ExportUsageReport(
        [FromQuery] string          format   = "csv",
        [FromQuery] DateTimeOffset? dateFrom = null,
        [FromQuery] DateTimeOffset? dateTo   = null,
        CancellationToken ct = default)
    {
        var (bearer, userToken) = await GetAuthAsync(ct);
        var upstream    = await _cargoDry.ExportUsageReportAsync(format, dateFrom, dateTo, bearer, userToken, ct);
        var bytes       = await upstream.Content.ReadAsByteArrayAsync(ct);
        var contentType = upstream.Content.Headers.ContentType?.ToString() ?? "text/csv";
        return File(bytes, contentType, $"cargodry-kit-usage-{DateTimeOffset.UtcNow:yyyyMMdd}.{format}");
    }

    /// <summary>GET /api/v1/admin-panel/cargodry/analytics</summary>
    [HttpGet("analytics")]
    [ProducesResponseType(typeof(CargoDryAnalyticsBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryAnalyticsBffDto>> GetAnalytics(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetCargoDryAnalyticsBffQuery(), ct);
        return SetResponse(result?.Analytics);
    }
}
