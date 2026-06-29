using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.CreateCargoDryProduct;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.ExtendKit;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.GenerateCargoDryBatch;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.RenewKit;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.RevokeBatch;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.RevokeKit;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.TransferKit;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.UpdateCargoDryProduct;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.ExportCargoDryBatches;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.ExportCargoDryKits;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.ExportCargoDryUsageReport;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryAnalytics;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryBatchByCode;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryBatchList;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryKitList;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryProductDetail;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryProducts;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryStats;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryStatsComparison;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryUsageReport;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryWarehouseOptions;
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

    // ── Kits ─────────────────────────────────────────────────────────────────

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

    /// <summary>GET /api/v1/admin-panel/cargodry/kits/export — CSV download</summary>
    [HttpGet("kits/export")]
    public async Task<IActionResult> ExportKits(
        [FromQuery] string? status    = null,
        [FromQuery] string? search    = null,
        [FromQuery] long?   vesselId  = null,
        [FromQuery] string? batchCode = null,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new ExportCargoDryKitsBffQuery
            {
                Status    = status,
                Search    = search,
                VesselId  = vesselId,
                BatchCode = batchCode,
            }, ct);

        if (result is null) return BadRequest();
        return File(result.Bytes, result.ContentType, result.FileName);
    }

    // ── Kit Actions ───────────────────────────────────────────────────────────

    /// <summary>POST /api/v1/admin-panel/cargodry/kits/{id}/transfer</summary>
    [HttpPost("kits/{id:long}/transfer")]
    [ProducesResponseType(typeof(TransferKitBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<TransferKitBffResponse>> TransferKit(
        long id, [FromBody] TransferKitBodyRequest body, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new TransferCargoDryKitBffCommand
            {
                KitId       = id,
                NewUserId   = body.NewUserId,
                NewVesselId = body.NewVesselId,
            }, ct);

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

    // ── Stats ─────────────────────────────────────────────────────────────────

    /// <summary>GET /api/v1/admin-panel/cargodry/stats</summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(CargoDryStatsBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryStatsBffDto>> GetStats(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetCargoDryStatsBffQuery(), ct);
        return SetResponse(result?.Stats);
    }

    /// <summary>GET /api/v1/admin-panel/cargodry/stats/comparison — prior-period KPI deltas</summary>
    [HttpGet("stats/comparison")]
    [ProducesResponseType(typeof(CargoDryStatsComparisonBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryStatsComparisonBffDto>> GetStatsComparison(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetCargoDryStatsComparisonBffQuery(), ct);
        return SetResponse(result?.Comparison);
    }

    // ── Analytics ─────────────────────────────────────────────────────────────

    /// <summary>GET /api/v1/admin-panel/cargodry/analytics</summary>
    [HttpGet("analytics")]
    [ProducesResponseType(typeof(CargoDryAnalyticsBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryAnalyticsBffDto>> GetAnalytics(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetCargoDryAnalyticsBffQuery(), ct);
        return SetResponse(result?.Analytics);
    }

    // ── Products ──────────────────────────────────────────────────────────────

    /// <summary>GET /api/v1/admin-panel/cargodry/products</summary>
    [HttpGet("products")]
    [ProducesResponseType(typeof(List<CargoDryProductBffDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<CargoDryProductBffDto>>> GetProducts(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetCargoDryProductsBffQuery(), ct);
        return SetResponse(result?.Products);
    }

    /// <summary>GET /api/v1/admin-panel/cargodry/products/{productCode}</summary>
    [HttpGet("products/{productCode}")]
    [ProducesResponseType(typeof(CargoDryProductBffDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductDetail(string productCode, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new GetCargoDryProductDetailBffQuery { ProductCode = productCode }, ct);

        if (result?.Product is null) return NotFound();
        return Ok(SetResponse(result.Product));
    }

    /// <summary>POST /api/v1/admin-panel/cargodry/products</summary>
    [HttpPost("products")]
    [ProducesResponseType(typeof(CargoDryProductBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryProductBffDto>> CreateProduct(
        [FromBody] CreateCargoDryProductBffCommand command, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(command, ct);
        return SetResponse(result?.Product);
    }

    /// <summary>PUT /api/v1/admin-panel/cargodry/products/{productCode}</summary>
    [HttpPut("products/{productCode}")]
    [ProducesResponseType(typeof(CargoDryProductBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryProductBffDto>> UpdateProduct(
        string productCode,
        [FromBody] UpdateProductBody body,
        CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new UpdateCargoDryProductBffCommand
            {
                ProductCode    = productCode,
                Name           = body.Name,
                Description    = body.Description,
                ValidityDays   = body.ValidityDays,
                RetailPrice    = body.RetailPrice,
                CurrencyCode   = body.CurrencyCode,
                HasSmartDevice = body.HasSmartDevice,
                DeviceType     = body.DeviceType,
                IsActive       = body.IsActive,
            }, ct);

        return SetResponse(result?.Product);
    }

    // ── Batches ───────────────────────────────────────────────────────────────

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

    /// <summary>GET /api/v1/admin-panel/cargodry/batches/export — CSV download</summary>
    [HttpGet("batches/export")]
    public async Task<IActionResult> ExportBatches(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new ExportCargoDryBatchesBffQuery(), ct);

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

    /// <summary>POST /api/v1/admin-panel/cargodry/batches/{batchCode}/revoke</summary>
    [HttpPost("batches/{batchCode}/revoke")]
    [ProducesResponseType(typeof(RevokeBatchBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RevokeBatchBffResponse>> RevokeBatch(
        string batchCode, [FromBody] RevokeBatchBodyRequest body, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new RevokeCargoDryBatchBffCommand { BatchCode = batchCode, Reason = body.Reason }, ct);

        return SetResponse(result?.Result);
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

    // ── Warehouses ────────────────────────────────────────────────────────────

    /// <summary>GET /api/v1/admin-panel/cargodry/warehouses</summary>
    [HttpGet("warehouses")]
    [ProducesResponseType(typeof(List<CargoDryWarehouseOptionBffDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<CargoDryWarehouseOptionBffDto>>> GetWarehouses(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetCargoDryWarehouseOptionsBffQuery(), ct);
        return SetResponse(result?.Warehouses);
    }

    // ── Reports ───────────────────────────────────────────────────────────────

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
}

// ── Inline body request records ───────────────────────────────────────────────

public sealed record RevokeKitBodyRequest(string Reason);
public sealed record RevokeBatchBodyRequest(string Reason);
public sealed record TransferKitBodyRequest(long NewUserId, long NewVesselId);
public sealed record RenewKitBodyRequest(int AddedDays, string? PaymentRef);
public sealed record ExtendKitBodyRequest(int AddedDays);

public sealed class UpdateProductBody
{
    public string  Name           { get; init; } = default!;
    public string  Description    { get; init; } = default!;
    public int     ValidityDays   { get; init; }
    public decimal RetailPrice    { get; init; }
    public string  CurrencyCode   { get; init; } = default!;
    public bool    HasSmartDevice { get; init; }
    public string? DeviceType     { get; init; }
    public bool    IsActive       { get; init; }
}
