using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Application.Commands.GenerateBatch;
using Aizen.Modules.CargoDry.Application.Commands.RenewKit;
using Aizen.Modules.CargoDry.Application.Commands.RevokeKit;
using Aizen.Modules.CargoDry.Application.Queries.GetAdminKitList;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryAnalytics;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryProductList;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryStats;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryUsageReport;
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

    [HttpGet("kits")]
    public async Task<IActionResult> GetKits(
        [FromQuery] CargoDryKitStatus? status,
        [FromQuery] string? search,
        [FromQuery] long?   vesselId = null,
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetAdminKitListQuery
        {
            Status   = status,
            Search   = search,
            VesselId = vesselId,
            Page     = page,
            PageSize = pageSize,
        }, ct);
        return Ok(result);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var result = await _sender.Send(new GetCargoDryStatsQuery(), ct);
        return Ok(result);
    }

    [HttpPost("batches/generate")]
    public async Task<IActionResult> GenerateBatch(
        [FromBody] GenerateBatchRequest request, CancellationToken ct)
    {
        var adminId = _info.UserInfoAccessor.UserInfo.UserId;
        var result = await _sender.Send(new GenerateBatchCommand
        {
            ProductCode = request.ProductCode,
            Count       = request.Count,
            AdminUserId = adminId,
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
            var csv = BuildCsv(result.Report);
            return File(
                System.Text.Encoding.UTF8.GetBytes(csv),
                "text/csv",
                $"cargodry-kit-usage-{DateTimeOffset.UtcNow:yyyyMMdd}.csv");
        }

        return Ok(result.Report);
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts(CancellationToken ct)
    {
        var result = await _sender.Send(new GetCargoDryProductListQuery(), ct);
        return Ok(result);
    }

    [HttpGet("analytics")]
    public async Task<IActionResult> GetAnalytics(CancellationToken ct)
    {
        var result = await _sender.Send(new GetCargoDryAnalyticsQuery(), ct);
        return Ok(result.Analytics);
    }

    private static string BuildCsv(Aizen.Modules.CargoDry.Abstraction.Dto.CargoDryKitUsageReportDto r)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("ProductCode,ProductName,TotalKits,ActivatedKits,ExpiredKits,RenewedKits,AvgEfficiencyPct");
        foreach (var row in r.ByProduct)
            sb.AppendLine($"{row.ProductCode},{row.ProductName},{row.TotalKits},{row.ActivatedKits}," +
                          $"{row.ExpiredKits},{row.RenewedKits},{row.AvgEfficiencyPct:F1}");
        return sb.ToString();
    }
}

public sealed class GenerateBatchRequest
{
    public string ProductCode { get; init; } = default!;
    public int    Count       { get; init; }
}

public sealed class RevokeKitRequest
{
    public string Reason { get; init; } = default!;
}

public sealed class ExtendKitRequest
{
    public int AddedDays { get; init; }
}
