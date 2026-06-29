using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.ExtendKit;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.GenerateCargoDryBatch;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.RenewKit;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.RevokeKit;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.ExportCargoDryUsageReport;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryAnalytics;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryBatchByCode;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryBatchList;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryKitList;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryProducts;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryStats;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryUsageReport;
using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

[ApiController]
[Route("api/v1/admin-panel/cargodry")]
[Tags("Admin Panel - CargoDry")]
[Authorize(Policy = "AdminPanelAccess")]
public sealed class AdminCargoDryController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public AdminCargoDryController(
        IHttpContextAccessor httpContextAccessor,
        IAizenCQRSProcessor  cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    /// <summary>GET /api/v1/admin-panel/cargodry/kits</summary>
    [HttpGet("kits")]
    [ProducesResponseType(typeof(CargoDryKitListBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryKitListBffDto>> GetKits(
        [FromQuery] string? status    = null,
        [FromQuery] string? search    = null,
        [FromQuery] long?   vesselId  = null,
        [FromQuery] string? batchCode = null,
        [FromQuery] int     page      = 1,
        [FromQuery] int     pageSize  = 25,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetCargoDryKitListBffQuery
            {
                Status    = status,
                Search    = search,
                VesselId  = vesselId,
                BatchCode = batchCode,
                Page      = page,
                PageSize  = pageSize,
            }, ct);

        return SetResponse(result?.KitList);
    }

    /// <summary>GET /api/v1/admin-panel/cargodry/stats</summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(CargoDryStatsBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryStatsBffDto>> GetStats(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetCargoDryStatsBffQuery(), ct);
        return SetResponse(result?.Stats);
    }

    /// <summary>POST /api/v1/admin-panel/cargodry/batches/generate</summary>
    [HttpPost("batches/generate")]
    [ProducesResponseType(typeof(GenerateBatchBffResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GenerateBatchBffResultDto>> GenerateBatch(
        [FromBody] GenerateCargoDryBatchBffCommand command, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(command, ct);
        return SetResponse(result?.Result);
    }

    /// <summary>POST /api/v1/admin-panel/cargodry/kits/{id}/revoke</summary>
    [HttpPost("kits/{id:long}/revoke")]
    [ProducesResponseType(typeof(RevokeKitBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RevokeKitBffResponse>> RevokeKit(
        long id, [FromBody] RevokeKitBodyRequest body, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new RevokeKitBffCommand { KitId = id, Reason = body.Reason }, ct);

        return SetResponse(result?.Result);
    }

    /// <summary>GET /api/v1/admin-panel/cargodry/products</summary>
    [HttpGet("products")]
    [ProducesResponseType(typeof(List<CargoDryProductBffDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<CargoDryProductBffDto>>> GetProducts(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetCargoDryProductsBffQuery(), ct);
        return SetResponse(result?.Products);
    }

    /// <summary>GET /api/v1/admin-panel/cargodry/batches</summary>
    [HttpGet("batches")]
    [ProducesResponseType(typeof(CargoDryBatchListBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryBatchListBffDto>> GetBatches(
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetCargoDryBatchListBffQuery { Page = page, PageSize = pageSize }, ct);

        return SetResponse(result?.BatchList);
    }

    /// <summary>POST /api/v1/admin-panel/cargodry/kits/{id}/renew</summary>
    [HttpPost("kits/{id:long}/renew")]
    [ProducesResponseType(typeof(CargoDryKitBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryKitBffDto>> RenewKit(
        long id, [FromBody] RenewKitBodyRequest body, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new RenewKitBffCommand
            {
                KitId      = id,
                AddedDays  = body.AddedDays,
                PaymentRef = body.PaymentRef,
            }, ct);

        return SetResponse(result?.Kit);
    }

    /// <summary>POST /api/v1/admin-panel/cargodry/kits/{id}/extend</summary>
    [HttpPost("kits/{id:long}/extend")]
    [ProducesResponseType(typeof(CargoDryKitBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryKitBffDto>> ExtendKit(
        long id, [FromBody] ExtendKitBodyRequest body, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new ExtendKitBffCommand { KitId = id, AddedDays = body.AddedDays }, ct);

        return SetResponse(result?.Kit);
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
        var result = await _cqrs.ProcessAsync(
            new ExportCargoDryUsageReportBffQuery
            {
                Format   = format,
                DateFrom = dateFrom,
                DateTo   = dateTo,
            }, ct);

        if (result is null) return BadRequest();

        return File(result.Bytes, result.ContentType, result.FileName);
    }

    /// <summary>GET /api/v1/admin-panel/cargodry/batches/{batchCode}</summary>
    [HttpGet("batches/{batchCode}")]
    [ProducesResponseType(typeof(CargoDryBatchBffDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBatchByCode(string batchCode, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new GetCargoDryBatchByCodeBffQuery { BatchCode = batchCode }, ct);

        if (result?.Batch is null) return NotFound();
        return Ok(SetResponse(result.Batch));
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

// ── Inline body request records (replaces CargoDry-specific request DTOs in controller layer) ──

public sealed record RevokeKitBodyRequest(string Reason);
public sealed record RenewKitBodyRequest(int AddedDays, string? PaymentRef);
public sealed record ExtendKitBodyRequest(int AddedDays);
