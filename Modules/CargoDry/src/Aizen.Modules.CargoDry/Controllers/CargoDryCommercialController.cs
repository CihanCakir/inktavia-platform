using Aizen.Modules.CargoDry.Abstraction.Enum;
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
}
