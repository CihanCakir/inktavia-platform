using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Application.Commands.ResolveCargoDrySalesAttributionFinancials;
using Aizen.Modules.CargoDry.Application.Commands.ResolveMonthlySellThroughSettlement;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDrySalesAttributionDetail;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDrySalesAttributionsPaged;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDrySellThroughSettlementDetail;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDrySellThroughSettlementsPaged;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.CargoDry.Controllers;

[ApiController]
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/v1/cargodry/admin/commercial")]
public sealed class CargoDryCommercialController : ControllerBase
{
    private readonly ISender _sender;

    public CargoDryCommercialController(ISender sender)
        => _sender = sender;

    // ── Sales Attribution ─────────────────────────────────────────────────────

    /// <summary>
    /// GET /api/v1/cargodry/admin/commercial/sales-attributions
    /// Paged list of sales attribution records.
    /// </summary>
    [HttpGet("sales-attributions")]
    public async Task<IActionResult> GetAttributionsPaged(
        [FromQuery] long?                           providerProfileId       = null,
        [FromQuery] string?                         productCode             = null,
        [FromQuery] string?                         batchCode               = null,
        [FromQuery] SalesChannel?                   salesChannel            = null,
        [FromQuery] CargoDryCommercialModel?        commercialModel         = null,
        [FromQuery] CargoDrySalesAttributionStatus? status                  = null,
        [FromQuery] long?                           sellThroughSettlementId = null,
        [FromQuery] DateTime?                       dateFrom                = null,
        [FromQuery] DateTime?                       dateTo                  = null,
        [FromQuery] string?                         search                  = null,
        [FromQuery] int                             page                    = 1,
        [FromQuery] int                             pageSize                = 25,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetCargoDrySalesAttributionsPagedQuery
        {
            ProviderProfileId       = providerProfileId,
            ProductCode             = productCode,
            BatchCode               = batchCode,
            SalesChannel            = salesChannel,
            CommercialModel         = commercialModel,
            Status                  = status,
            SellThroughSettlementId = sellThroughSettlementId,
            DateFrom                = dateFrom,
            DateTo                  = dateTo,
            Search                  = search,
            Page                    = page,
            PageSize                = pageSize,
        }, ct);

        return Ok(result.PagedResult);
    }

    /// <summary>
    /// GET /api/v1/cargodry/admin/commercial/sales-attributions/{id}
    /// Full detail of a single sales attribution record.
    /// </summary>
    [HttpGet("sales-attributions/{id:long}")]
    public async Task<IActionResult> GetAttributionDetail(long id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetCargoDrySalesAttributionDetailQuery { Id = id }, ct);
        if (result.Detail is null) return NotFound();
        return Ok(result.Detail);
    }

    // ── Sell-Through Settlements ──────────────────────────────────────────────

    /// <summary>
    /// GET /api/v1/cargodry/admin/commercial/settlements
    /// Paged list of sell-through settlement records.
    /// </summary>
    [HttpGet("settlements")]
    public async Task<IActionResult> GetSettlementsPaged(
        [FromQuery] long?                                providerProfileId      = null,
        [FromQuery] long?                                consignmentAgreementId = null,
        [FromQuery] string?                              productCode            = null,
        [FromQuery] CargoDrySellThroughSettlementStatus? status                 = null,
        [FromQuery] DateTime?                            periodFrom             = null,
        [FromQuery] DateTime?                            periodTo               = null,
        [FromQuery] string?                              search                 = null,
        [FromQuery] int                                  page                   = 1,
        [FromQuery] int                                  pageSize               = 25,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetCargoDrySellThroughSettlementsPagedQuery
        {
            ProviderProfileId      = providerProfileId,
            ConsignmentAgreementId = consignmentAgreementId,
            ProductCode            = productCode,
            Status                 = status,
            PeriodFrom             = periodFrom,
            PeriodTo               = periodTo,
            Search                 = search,
            Page                   = page,
            PageSize               = pageSize,
        }, ct);

        return Ok(result.PagedResult);
    }

    /// <summary>
    /// GET /api/v1/cargodry/admin/commercial/settlements/{id}
    /// Full detail of a single sell-through settlement record.
    /// </summary>
    [HttpGet("settlements/{id:long}")]
    public async Task<IActionResult> GetSettlementDetail(long id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetCargoDrySellThroughSettlementDetailQuery { Id = id }, ct);
        if (result.Detail is null) return NotFound();
        return Ok(result.Detail);
    }

    // ── Phase 4A: Financial Resolution ───────────────────────────────────────

    /// <summary>
    /// POST /api/v1/cargodry/admin/commercial/sales-attributions/{id}/resolve-financials
    /// Resolves financial amounts (SalePrice, CommissionRate, ProviderShareAmount, PlatformShareAmount)
    /// for a single sales attribution record.
    /// </summary>
    [HttpPost("sales-attributions/{id:long}/resolve-financials")]
    public async Task<IActionResult> ResolveAttributionFinancials(
        long id,
        [FromBody] ResolveAttributionFinancialsRequest body,
        CancellationToken ct)
    {
        var result = await _sender.Send(new ResolveCargoDrySalesAttributionFinancialsCommand
        {
            SalesAttributionId    = id,
            SalePrice             = body.SalePrice,
            CurrencyCode          = body.CurrencyCode,
            CommissionRateOverride = body.CommissionRateOverride,
            ResolvedByUserId      = body.ResolvedByUserId,
            ResolutionNote        = body.ResolutionNote,
        }, ct);

        return Ok(result.Attribution);
    }

    /// <summary>
    /// POST /api/v1/cargodry/admin/commercial/settlements/{id}/resolve-monthly
    /// Finalizes a monthly settlement: verifies all attributions are resolved,
    /// recalculates totals, and marks the settlement ReadyForSettlement.
    /// </summary>
    [HttpPost("settlements/{id:long}/resolve-monthly")]
    public async Task<IActionResult> ResolveMonthlySettlement(
        long id,
        [FromBody] ResolveMonthlySettlementRequest body,
        CancellationToken ct)
    {
        var result = await _sender.Send(new ResolveMonthlySellThroughSettlementCommand
        {
            SettlementId     = id,
            ResolvedByUserId = body.ResolvedByUserId,
            ResolutionNote   = body.ResolutionNote,
        }, ct);

        return Ok(result.Settlement);
    }
}

// ── Inline request models (Phase 4A — module layer) ──────────────────────────

public sealed class ResolveAttributionFinancialsRequest
{
    public decimal  SalePrice              { get; init; }
    public string   CurrencyCode           { get; init; } = default!;
    public decimal? CommissionRateOverride  { get; init; }
    public long     ResolvedByUserId       { get; init; }
    public string?  ResolutionNote         { get; init; }
}

public sealed class ResolveMonthlySettlementRequest
{
    public long    ResolvedByUserId { get; init; }
    public string? ResolutionNote   { get; init; }
}
