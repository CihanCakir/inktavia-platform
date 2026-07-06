using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Application.Commands.CancelCargoDryRenewalPreparation;
using Aizen.Modules.CargoDry.Application.Commands.CompleteCargoDryRenewal;
using Aizen.Modules.CargoDry.Application.Commands.CreateCargoDryProduct;
using Aizen.Modules.CargoDry.Application.Commands.DispatchCargoDryRenewalNotification;
using Aizen.Modules.CargoDry.Application.Commands.GenerateBatch;
using Aizen.Modules.CargoDry.Application.Commands.PrepareCargoDryKitRenewal;
using Aizen.Modules.CargoDry.Application.Commands.PrepareCargoDryRenewalInvoice;
using Aizen.Modules.CargoDry.Application.Commands.PrepareCargoDryRenewalNotification;
using Aizen.Modules.CargoDry.Application.Commands.RenewKit;
using Aizen.Modules.CargoDry.Application.Commands.RevokeBatch;
using Aizen.Modules.CargoDry.Application.Commands.RevokeKit;
using Aizen.Modules.CargoDry.Application.Commands.TransferKit;
using Aizen.Modules.CargoDry.Application.Commands.UpdateCargoDryProduct;
using Aizen.Modules.CargoDry.Application.Queries.GetAdminKitList;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryKitDetail;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryKitLifecycleHistory;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryKitLifecycleEventsPaged;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryOperationalAlerts;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryOperationalOverview;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryOpportunityRoutingCandidates;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryRenewalCandidates;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryRenewalPreparationDetail;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryRenewalPreparationsPaged;
using Aizen.Modules.CargoDry.Application.Queries.LookupCargoDryKitAdmin;
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
    public CargoDryAdminController(ISender sender)
    {
        _sender = sender;
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

    [HttpGet("kits/lookup")]
    public async Task<IActionResult> LookupKit(
        [FromQuery] string q,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new LookupCargoDryKitAdminQuery { Query = q }, ct);
        return Ok(result);
    }

    [HttpGet("kits/{id:long}")]
    public async Task<IActionResult> GetKitDetail(
        long id,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetCargoDryKitDetailQuery { KitId = id }, ct);
        if (result.Kit is null) return NotFound();
        return Ok(result);
    }

    // ── Phase 9: Kit lifecycle history ───────────────────────────────────────

    [HttpGet("kits/{id:long}/history")]
    public async Task<IActionResult> GetKitLifecycleHistory(
        long id,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(
            new GetCargoDryKitLifecycleHistoryQuery { KitId = id }, ct);
        return Ok(result);
    }

    [HttpGet("kits/lifecycle-events")]
    public async Task<IActionResult> GetKitLifecycleEvents(
        [FromQuery] long?                         kitId       = null,
        [FromQuery] string?                       kitCode     = null,
        [FromQuery] string?                       batchCode   = null,
        [FromQuery] string?                       productCode = null,
        [FromQuery] CargoDryKitLifecycleEventType? eventType  = null,
        [FromQuery] long?                         actorUserId = null,
        [FromQuery] DateTimeOffset?               dateFrom    = null,
        [FromQuery] DateTimeOffset?               dateTo      = null,
        [FromQuery] int                           page        = 1,
        [FromQuery] int                           pageSize    = 25,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetCargoDryKitLifecycleEventsPagedQuery
        {
            KitId       = kitId,
            KitCode     = kitCode,
            BatchCode   = batchCode,
            ProductCode = productCode,
            EventType   = eventType,
            ActorUserId = actorUserId,
            DateFrom    = dateFrom,
            DateTo      = dateTo,
            Page        = page,
            PageSize    = pageSize,
        }, ct);
        return Ok(result);
    }

    [HttpGet("kits/operational-alerts")]
    public async Task<IActionResult> GetOperationalAlerts(
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetCargoDryOperationalAlertsQuery
        {
            Page     = page,
            PageSize = pageSize,
        }, ct);
        return Ok(result);
    }

    // ── Phase 9: Operational overview KPI ────────────────────────────────────

    [HttpGet("operational-overview")]
    public async Task<IActionResult> GetOperationalOverview(CancellationToken ct)
    {
        var result = await _sender.Send(new GetCargoDryOperationalOverviewQuery(), ct);
        return Ok(result);
    }

    // ── Phase 24: Opportunity routing candidates (read-only, decision support) ─

    /// <summary>
    /// Phase 24 — Returns distinct provider profile IDs that have active CargoDry
    /// relationships (Active ConsignmentAgreements + ProviderInventory).
    /// Used by AdminPanel BFF for CargoDryOpportunityRouting priority preview.
    /// Read-only — no writes, no enforcement, no scoring changes.
    /// </summary>
    [HttpGet("opportunity-routing/candidates")]
    public async Task<IActionResult> GetOpportunityRoutingCandidates(CancellationToken ct)
    {
        var result = await _sender.Send(new GetCargoDryOpportunityRoutingCandidatesQuery(), ct);
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
        var result = await _sender.Send(new TransferCargoDryKitCommand
        {
            KitId       = id,
            NewUserId   = request.NewUserId,
            NewVesselId = request.NewVesselId,
        }, ct);
        return Ok(result);
    }

    [HttpPost("kits/{id:long}/revoke")]
    public async Task<IActionResult> RevokeKit(
        long id, [FromBody] RevokeKitRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new RevokeKitCommand
        {
            KitId  = id,
            Reason = request.Reason,
        }, ct);
        return Ok(result);
    }

    [HttpPost("kits/{id:long}/extend")]
    public async Task<IActionResult> ExtendKit(
        long id, [FromBody] ExtendKitRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new RenewKitCommand
        {
            KitId     = id,
            AddedDays = request.AddedDays,
            Type      = RenewalType.AdminExtension,
        }, ct);
        return Ok(result);
    }

    [HttpPost("kits/{id:long}/renew")]
    public async Task<IActionResult> RenewKit(
        long id, [FromBody] RenewKitRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new RenewKitCommand
        {
            KitId      = id,
            AddedDays  = request.AddedDays,
            PaymentRef = request.PaymentRef,
            Type       = RenewalType.AdminExtension,
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
        var result = await _sender.Send(new RevokeCargoDryBatchCommand
        {
            BatchCode = batchCode,
            Reason    = request.Reason,
        }, ct);
        return Ok(result);
    }

    [HttpPost("batches/generate")]
    public async Task<IActionResult> GenerateBatch(
        [FromBody] GenerateBatchRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new GenerateBatchCommand
        {
            ProductCode     = request.ProductCode,
            Count           = request.Count,
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

    // ─── Renewals ────────────────────────────────────────────────────────────

    /// <summary>GET /renewals/candidates — expiring kits eligible for renewal</summary>
    [HttpGet("renewals/candidates")]
    public async Task<IActionResult> GetRenewalCandidates(
        [FromQuery] int withinDays = 90,
        [FromQuery] int page       = 1,
        [FromQuery] int pageSize   = 50,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetCargoDryRenewalCandidatesQuery
        {
            WithinDays = withinDays,
            Page       = page,
            PageSize   = pageSize,
        }, ct);
        return Ok(result);
    }

    /// <summary>POST /renewals — create a new renewal preparation for a kit</summary>
    [HttpPost("renewals")]
    public async Task<IActionResult> PrepareRenewal(
        [FromBody] PrepareRenewalRequest req,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new PrepareCargoDryKitRenewalCommand
        {
            KitId                  = req.KitId,
            RequestedRenewalMonths = req.RequestedRenewalMonths,
            Note                   = req.Note,
        }, ct);
        return Ok(result);
    }

    /// <summary>GET /renewals — paged list of renewal preparations</summary>
    [HttpGet("renewals")]
    public async Task<IActionResult> GetRenewalPreparations(
        [FromQuery] long?                              kitId              = null,
        [FromQuery] string?                            kitCode            = null,
        [FromQuery] string?                            productCode        = null,
        [FromQuery] long?                              ownerUserId        = null,
        [FromQuery] long?                              vesselId           = null,
        [FromQuery] CargoDryRenewalPreparationStatus?  status             = null,
        [FromQuery] CargoDryRenewalNotificationStatus? notificationStatus = null,
        [FromQuery] DateTimeOffset?                    preparedFrom       = null,
        [FromQuery] DateTimeOffset?                    preparedTo         = null,
        [FromQuery] int                                page               = 1,
        [FromQuery] int                                pageSize           = 25,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetCargoDryRenewalPreparationsPagedQuery
        {
            KitId              = kitId,
            KitCode            = kitCode,
            ProductCode        = productCode,
            OwnerUserId        = ownerUserId,
            VesselId           = vesselId,
            Status             = status,
            NotificationStatus = notificationStatus,
            PreparedFrom       = preparedFrom,
            PreparedTo         = preparedTo,
            Page               = page,
            PageSize           = pageSize,
        }, ct);
        return Ok(result);
    }

    /// <summary>GET /renewals/{id} — single renewal preparation by id</summary>
    [HttpGet("renewals/{id:long}")]
    public async Task<IActionResult> GetRenewalPreparationDetail(
        long id,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetCargoDryRenewalPreparationDetailQuery { Id = id }, ct);
        if (result is null) return NotFound();
        return Ok(result);
    }

    /// <summary>POST /renewals/{id}/invoice — prepare the invoice for a renewal preparation</summary>
    [HttpPost("renewals/{id:long}/invoice")]
    public async Task<IActionResult> PrepareRenewalInvoice(
        long id,
        [FromBody] PrepareRenewalInvoiceRequest req,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new PrepareCargoDryRenewalInvoiceCommand
        {
            RenewalPreparationId = id,
            Note                 = req.Note,
        }, ct);
        return Ok(result);
    }

    /// <summary>POST /renewals/{id}/notification/prepare — configure notification template and channels</summary>
    [HttpPost("renewals/{id:long}/notification/prepare")]
    public async Task<IActionResult> PrepareRenewalNotification(
        long id,
        [FromBody] PrepareRenewalNotificationRequest req,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new PrepareCargoDryRenewalNotificationCommand
        {
            RenewalPreparationId = id,
            TemplateCode         = req.TemplateCode,
            LanguageCode         = req.LanguageCode,
            ChannelsJson         = req.ChannelsJson,
        }, ct);
        return Ok(result);
    }

    /// <summary>POST /renewals/{id}/notification/dispatch — publish renewal notification to message bus</summary>
    [HttpPost("renewals/{id:long}/notification/dispatch")]
    public async Task<IActionResult> DispatchRenewalNotification(
        long id,
        [FromBody] DispatchRenewalNotificationRequest req,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new DispatchCargoDryRenewalNotificationCommand
        {
            RenewalPreparationId = id,
            RecipientEmail       = req.RecipientEmail,
            RecipientPhone       = req.RecipientPhone,
        }, ct);
        return Ok(result);
    }

    /// <summary>POST /renewals/{id}/complete — complete renewal (triggers RenewKitCommand internally)</summary>
    [HttpPost("renewals/{id:long}/complete")]
    public async Task<IActionResult> CompleteRenewal(
        long id,
        [FromBody] CompleteRenewalRequest req,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new CompleteCargoDryRenewalCommand
        {
            RenewalPreparationId   = id,
            ManualPaymentReference = req.ManualPaymentReference,
            Note                   = req.Note,
        }, ct);
        return Ok(result);
    }

    /// <summary>POST /renewals/{id}/cancel — cancel an in-progress renewal preparation</summary>
    [HttpPost("renewals/{id:long}/cancel")]
    public async Task<IActionResult> CancelRenewal(
        long id,
        [FromBody] CancelRenewalRequest req,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new CancelCargoDryRenewalPreparationCommand
        {
            RenewalPreparationId = id,
            CancellationReason   = req.CancellationReason,
            Note                 = req.Note,
        }, ct);
        return Ok(result);
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

public sealed class PrepareRenewalRequest
{
    public long    KitId                  { get; init; }
    public int     RequestedRenewalMonths { get; init; }
    public string? Note                   { get; init; }
}

public sealed class PrepareRenewalInvoiceRequest
{
    public string? Note { get; init; }
}

public sealed class PrepareRenewalNotificationRequest
{
    public string  TemplateCode { get; init; } = default!;
    public string  LanguageCode { get; init; } = "tr";
    public string  ChannelsJson { get; init; } = "[\"Email\"]";
}

public sealed class DispatchRenewalNotificationRequest
{
    public string? RecipientEmail { get; init; }
    public string? RecipientPhone { get; init; }
}

public sealed class CompleteRenewalRequest
{
    public string? ManualPaymentReference { get; init; }
    public string? Note                   { get; init; }
}

public sealed class CancelRenewalRequest
{
    public string  CancellationReason { get; init; } = default!;
    public string? Note               { get; init; }
}
