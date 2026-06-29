using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Application.Commands.CreateCargoDryProduct;
using Aizen.Modules.CargoDry.Application.Commands.GenerateBatch;
using Aizen.Modules.CargoDry.Application.Commands.RenewKit;
using Aizen.Modules.CargoDry.Application.Commands.RevokeBatch;
using Aizen.Modules.CargoDry.Application.Commands.RevokeKit;
using Aizen.Modules.CargoDry.Application.Commands.TransferKit;
using Aizen.Modules.CargoDry.Application.Commands.UpdateCargoDryProduct;
using Aizen.Modules.CargoDry.Application.Queries.GetAdminKitList;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryAnalytics;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryBatchByCode;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryBatchList;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryProductDetail;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryProductList;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryStats;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryStatsComparison;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryUsageReport;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryWarehouseOptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.CargoDry.Controllers;

[ApiController]
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/v1/cargodry/admin")]
public sealed class CargoDryAdminController : ControllerBase
{
    private readonly ISender            _sender;
    private readonly IAizenInfoAccessor _info;

    public CargoDryAdminController(ISender sender, IAizenInfoAccessor info)
    {
        _sender = sender;
        _info   = info;
    }

    // ─── Kits ────────────────────────────────────────────────────────────────

    [HttpGet("kits")]
    public async Task<IActionResult> GetKits(
        [FromQuery] CargoDryKitStatus? status,
        [FromQuery] string? search,
        [FromQuery] long?   vesselId    = null,
        [FromQuery] long?   ownerUserId = null,
        [FromQuery] string? batchCode   = null,
        [FromQuery] int     page        = 1,
        [FromQuery] int     pageSize    = 25,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetAdminKitListQuery
        {
            Status      = status,
            Search      = search,
            VesselId    = vesselId,
            OwnerUserId = ownerUserId,
            BatchCode   = batchCode,
            Page        = page,
            PageSize    = pageSize,
        }, ct);
        return Ok(result);
    }

    [HttpGet("kits/export")]
    public async Task<IActionResult> ExportKits(
        [FromQuery] CargoDryKitStatus? status,
        [FromQuery] string? search,
        [FromQuery] long?   vesselId  = null,
        [FromQuery] string? batchCode = null,
        CancellationToken ct = default)
    {
        // Load all matching kits (no pagination for export)
        var result = await _sender.Send(new GetAdminKitListQuery
        {
            Status    = status,
            Search    = search,
            VesselId  = vesselId,
            BatchCode = batchCode,
            Page      = 1,
            PageSize  = int.MaxValue,
        }, ct);

        var csv = BuildKitsCsv(result);
        return File(
            System.Text.Encoding.UTF8.GetBytes(csv),
            "text/csv",
            $"cargodry-kits-{DateTimeOffset.UtcNow:yyyyMMdd}.csv");
    }

    [HttpPost("kits/{id:long}/transfer")]
    public async Task<IActionResult> TransferKit(
        long id, [FromBody] TransferKitRequest request, CancellationToken ct)
    {
        var adminId = _info.UserInfoAccessor.UserInfo.UserId;
        var result = await _sender.Send(new TransferCargoDryKitCommand
        {
            KitId       = id,
            NewUserId   = request.NewUserId,
            NewVesselId = request.NewVesselId,
            AdminId     = adminId,
        }, ct);
        return Ok(result);
    }

    [HttpPost("kits/{id:long}/revoke")]
    public async Task<IActionResult> RevokeKit(
        long id, [FromBody] RevokeKitRequest request, CancellationToken ct)
    {
        var adminId = _info.UserInfoAccessor.UserInfo.UserId;
        var result = await _sender.Send(new RevokeKitCommand
        {
            KitId       = id,
            Reason      = request.Reason,
            AdminUserId = adminId,
        }, ct);
        return Ok(result);
    }

    [HttpPost("kits/{id:long}/extend")]
    public async Task<IActionResult> ExtendKit(
        long id, [FromBody] ExtendKitRequest request, CancellationToken ct)
    {
        var adminId = _info.UserInfoAccessor.UserInfo.UserId;
        var result = await _sender.Send(new RenewKitCommand
        {
            KitId       = id,
            AddedDays   = request.AddedDays,
            Type        = RenewalType.AdminExtension,
            AdminUserId = adminId,
        }, ct);
        return Ok(result);
    }

    [HttpPost("kits/{id:long}/renew")]
    public async Task<IActionResult> RenewKit(
        long id, [FromBody] RenewKitRequest request, CancellationToken ct)
    {
        var adminId = _info.UserInfoAccessor.UserInfo.UserId;
        var result  = await _sender.Send(new RenewKitCommand
        {
            KitId       = id,
            AddedDays   = request.AddedDays,
            PaymentRef  = request.PaymentRef,
            Type        = RenewalType.AdminExtension,
            AdminUserId = adminId,
        }, ct);
        return Ok(result);
    }

    // ─── Stats ───────────────────────────────────────────────────────────────

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var result = await _sender.Send(new GetCargoDryStatsQuery(), ct);
        return Ok(result);
    }

    [HttpGet("stats/comparison")]
    public async Task<IActionResult> GetStatsComparison(CancellationToken ct)
    {
        var result = await _sender.Send(new GetCargoDryStatsComparisonQuery(), ct);
        return Ok(result);
    }

    // ─── Analytics ───────────────────────────────────────────────────────────

    [HttpGet("analytics")]
    public async Task<IActionResult> GetAnalytics(CancellationToken ct)
    {
        var result = await _sender.Send(new GetCargoDryAnalyticsQuery(), ct);
        return Ok(result.Analytics);
    }

    // ─── Products ────────────────────────────────────────────────────────────

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts(CancellationToken ct)
    {
        var result = await _sender.Send(new GetCargoDryProductListQuery(), ct);
        return Ok(result);
    }

    [HttpGet("products/{productCode}")]
    public async Task<IActionResult> GetProductDetail(string productCode, CancellationToken ct)
    {
        var result = await _sender.Send(
            new GetCargoDryProductDetailQuery { ProductCode = productCode }, ct);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpPost("products")]
    public async Task<IActionResult> CreateProduct(
        [FromBody] CreateProductRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new CreateCargoDryProductCommand
        {
            ProductCode    = request.ProductCode,
            Name           = request.Name,
            Description    = request.Description,
            ValidityDays   = request.ValidityDays,
            RetailPrice    = request.RetailPrice,
            CurrencyCode   = request.CurrencyCode,
            HasSmartDevice = request.HasSmartDevice,
            DeviceType     = request.DeviceType,
        }, ct);
        return CreatedAtAction(
            nameof(GetProductDetail),
            new { productCode = result.ProductCode },
            result);
    }

    [HttpPut("products/{productCode}")]
    public async Task<IActionResult> UpdateProduct(
        string productCode,
        [FromBody] UpdateProductRequest request,
        CancellationToken ct)
    {
        var result = await _sender.Send(new UpdateCargoDryProductCommand
        {
            ProductCode    = productCode,
            Name           = request.Name,
            Description    = request.Description,
            ValidityDays   = request.ValidityDays,
            RetailPrice    = request.RetailPrice,
            CurrencyCode   = request.CurrencyCode,
            HasSmartDevice = request.HasSmartDevice,
            DeviceType     = request.DeviceType,
            IsActive       = request.IsActive,
        }, ct);
        return Ok(result);
    }

    // ─── Batches ─────────────────────────────────────────────────────────────

    [HttpGet("batches")]
    public async Task<IActionResult> GetBatches(
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(
            new GetCargoDryBatchListQuery { Page = page, PageSize = pageSize }, ct);
        return Ok(result);
    }

    [HttpGet("batches/export")]
    public async Task<IActionResult> ExportBatches(CancellationToken ct)
    {
        var result = await _sender.Send(
            new GetCargoDryBatchListQuery { Page = 1, PageSize = int.MaxValue }, ct);

        var csv = BuildBatchesCsv(result.Items);
        return File(
            System.Text.Encoding.UTF8.GetBytes(csv),
            "text/csv",
            $"cargodry-batches-{DateTimeOffset.UtcNow:yyyyMMdd}.csv");
    }

    [HttpGet("batches/{batchCode}")]
    public async Task<IActionResult> GetBatchByCode(string batchCode, CancellationToken ct)
    {
        var result = await _sender.Send(
            new GetCargoDryBatchByCodeQuery { BatchCode = batchCode }, ct);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpPost("batches/{batchCode}/revoke")]
    public async Task<IActionResult> RevokeBatch(
        string batchCode, [FromBody] RevokeBatchRequest request, CancellationToken ct)
    {
        var adminId = _info.UserInfoAccessor.UserInfo.UserId;
        var result = await _sender.Send(new RevokeCargoDryBatchCommand
        {
            BatchCode = batchCode,
            Reason    = request.Reason,
            AdminId   = adminId,
        }, ct);
        return Ok(result);
    }

    [HttpPost("batches/generate")]
    public async Task<IActionResult> GenerateBatch(
        [FromBody] GenerateBatchRequest request, CancellationToken ct)
    {
        var adminId = _info.UserInfoAccessor.UserInfo.UserId;
        var result = await _sender.Send(new GenerateBatchCommand
        {
            ProductCode     = request.ProductCode,
            Count           = request.Count,
            AdminUserId     = adminId,
            BatchLabel      = request.BatchLabel,
            WarehouseCode   = request.WarehouseCode,
            ProductionNotes = request.ProductionNotes,
        }, ct);
        return Ok(result);
    }

    // ─── Warehouses ──────────────────────────────────────────────────────────

    [HttpGet("warehouses")]
    public async Task<IActionResult> GetWarehouses(CancellationToken ct)
    {
        var result = await _sender.Send(new GetCargoDryWarehouseOptionsQuery(), ct);
        return Ok(result);
    }

    // ─── Reports ─────────────────────────────────────────────────────────────

    [HttpGet("reports/usage")]
    public async Task<IActionResult> GetUsageReport(
        [FromQuery] DateTimeOffset? dateFrom,
        [FromQuery] DateTimeOffset? dateTo,
        CancellationToken ct)
    {
        var result = await _sender.Send(new GetCargoDryUsageReportQuery
        {
            DateFrom = dateFrom,
            DateTo   = dateTo,
        }, ct);
        return Ok(result.Report);
    }

    [HttpGet("reports/usage/export")]
    public async Task<IActionResult> ExportUsageReport(
        [FromQuery] string format,
        [FromQuery] DateTimeOffset? dateFrom,
        [FromQuery] DateTimeOffset? dateTo,
        CancellationToken ct)
    {
        var result = await _sender.Send(new GetCargoDryUsageReportQuery
        {
            DateFrom = dateFrom,
            DateTo   = dateTo,
        }, ct);

        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            var csv = BuildUsageCsv(result.Report);
            return File(
                System.Text.Encoding.UTF8.GetBytes(csv),
                "text/csv",
                $"cargodry-kit-usage-{DateTimeOffset.UtcNow:yyyyMMdd}.csv");
        }

        return Ok(result.Report);
    }

    // ─── CSV builders ────────────────────────────────────────────────────────

    private static string BuildKitsCsv(
        GetAdminKitListResponse r)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("KitCode,SerialNumber,ProductCode,ProductName,BatchCode,Status," +
                      "OwnerUserId,VesselId,ActivatedAt,ExpiresAt,RenewalCount,EfficiencyPercent");
        foreach (var k in r.Items)
            sb.AppendLine($"{k.KitCode},{k.SerialNumber},{k.ProductCode},{k.ProductName}," +
                          $"{k.BatchCode},{k.Status},{k.OwnerUserId},{k.VesselId}," +
                          $"{k.ActivatedAt:O},{k.ExpiresAt:O},{k.RenewalCount},{k.EfficiencyPercent:F1}");
        return sb.ToString();
    }

    private static string BuildBatchesCsv(
        IEnumerable<Aizen.Modules.CargoDry.Abstraction.Dto.CargoDryBatchDto> batches)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("BatchCode,ProductCode,ProductName,TotalKits,GeneratedAt," +
                      "WarehouseCode,BatchLabel,IsRevoked");
        foreach (var b in batches)
            sb.AppendLine($"{b.BatchCode},{b.ProductCode},{b.ProductName},{b.TotalKits}," +
                          $"{b.GeneratedAt},{b.WarehouseCode},{b.BatchLabel},{b.IsRevoked}");
        return sb.ToString();
    }

    private static string BuildUsageCsv(
        Aizen.Modules.CargoDry.Abstraction.Dto.CargoDryKitUsageReportDto r)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("ProductCode,ProductName,TotalKits,ActivatedKits,ExpiredKits,RenewedKits,AvgEfficiencyPct");
        foreach (var row in r.ByProduct)
            sb.AppendLine($"{row.ProductCode},{row.ProductName},{row.TotalKits},{row.ActivatedKits}," +
                          $"{row.ExpiredKits},{row.RenewedKits},{row.AvgEfficiencyPct:F1}");
        return sb.ToString();
    }
}

// ─── Request models ──────────────────────────────────────────────────────────

public sealed class GenerateBatchRequest
{
    public string  ProductCode     { get; init; } = default!;
    public int     Count           { get; init; }
    public string? BatchLabel      { get; init; }
    public string? WarehouseCode   { get; init; }
    public string? ProductionNotes { get; init; }
}

public sealed class CreateProductRequest
{
    public string  ProductCode    { get; init; } = default!;
    public string  Name           { get; init; } = default!;
    public string  Description    { get; init; } = default!;
    public int     ValidityDays   { get; init; }
    public decimal RetailPrice    { get; init; }
    public string  CurrencyCode   { get; init; } = default!;
    public bool    HasSmartDevice { get; init; }
    public string? DeviceType     { get; init; }
}

public sealed class UpdateProductRequest
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

public sealed class RevokeKitRequest
{
    public string Reason { get; init; } = default!;
}

public sealed class RevokeBatchRequest
{
    public string Reason { get; init; } = default!;
}

public sealed class TransferKitRequest
{
    public long NewUserId   { get; init; }
    public long NewVesselId { get; init; }
}

public sealed class ExtendKitRequest
{
    public int AddedDays { get; init; }
}

public sealed class RenewKitRequest
{
    public int     AddedDays  { get; init; }
    public string? PaymentRef { get; init; }
}
