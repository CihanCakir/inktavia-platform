using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Application.Commands.AdjustProviderInventory;
using Aizen.Modules.CargoDry.Application.Commands.AllocateBatchToProvider;
using Aizen.Modules.CargoDry.Application.Queries.GetBatchAllocationPreview;
using Aizen.Modules.CargoDry.Application.Queries.GetProviderInventoryDetail;
using Aizen.Modules.CargoDry.Application.Queries.GetProviderInventoryList;
using Aizen.Modules.CargoDry.Application.Queries.GetProviderInventoryMovements;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.CargoDry.Controllers;

[ApiController]
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/v1/cargodry/admin/inventory")]
public sealed class CargoDryProviderInventoryController : ControllerBase
{
    private readonly ISender _sender;

    public CargoDryProviderInventoryController(ISender sender)
        => _sender = sender;

    // ── Queries ───────────────────────────────────────────────────────────────

    /// <summary>
    /// GET /api/v1/cargodry/admin/inventory
    /// Paged list of provider inventory rows with optional filters.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] long?                    providerProfileId = null,
        [FromQuery] string?                  productCode       = null,
        [FromQuery] CargoDryCommercialModel? commercialModel   = null,
        [FromQuery] SalesChannel?            salesChannel      = null,
        [FromQuery] bool?                    hasAvailableStock = null,
        [FromQuery] string?                  search            = null,
        [FromQuery] int                      page              = 1,
        [FromQuery] int                      pageSize          = 25,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetProviderInventoryListQuery
        {
            ProviderProfileId = providerProfileId,
            ProductCode       = productCode,
            CommercialModel   = commercialModel,
            SalesChannel      = salesChannel,
            HasAvailableStock = hasAvailableStock,
            Search            = search,
            Page              = page,
            PageSize          = pageSize,
        }, ct);

        return Ok(result);
    }

    /// <summary>
    /// GET /api/v1/cargodry/admin/inventory/provider/{providerProfileId}
    /// Aggregate inventory detail for a single provider (all products/batches summed).
    /// </summary>
    [HttpGet("provider/{providerProfileId:long}")]
    public async Task<IActionResult> GetProviderDetail(long providerProfileId, CancellationToken ct)
    {
        var result = await _sender.Send(
            new GetProviderInventoryDetailQuery { ProviderProfileId = providerProfileId }, ct);

        return Ok(result);
    }

    /// <summary>
    /// GET /api/v1/cargodry/admin/inventory/movements
    /// Paged inventory movement ledger with optional provider/product/batch/type/date filters.
    /// </summary>
    [HttpGet("movements")]
    public async Task<IActionResult> GetMovements(
        [FromQuery] long?                  providerProfileId = null,
        [FromQuery] string?                productCode       = null,
        [FromQuery] string?                batchCode         = null,
        [FromQuery] InventoryMovementType? movementType      = null,
        [FromQuery] DateTime?              dateFrom          = null,
        [FromQuery] DateTime?              dateTo            = null,
        [FromQuery] int                    page              = 1,
        [FromQuery] int                    pageSize          = 50,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetProviderInventoryMovementsQuery
        {
            ProviderProfileId = providerProfileId,
            ProductCode       = productCode,
            BatchCode         = batchCode,
            MovementType      = movementType,
            DateFrom          = dateFrom,
            DateTo            = dateTo,
            Page              = page,
            PageSize          = pageSize,
        }, ct);

        return Ok(result);
    }

    /// <summary>
    /// GET /api/v1/cargodry/admin/inventory/preview?batchCode=X&amp;providerProfileId=Y&amp;commercialModel=Z
    /// Pre-flight validation for a batch→provider allocation. Returns CanAllocate + blocking reason.
    /// </summary>
    [HttpGet("preview")]
    public async Task<IActionResult> GetAllocationPreview(
        [FromQuery] string                  batchCode,
        [FromQuery] long                    providerProfileId,
        [FromQuery] CargoDryCommercialModel commercialModel,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetBatchAllocationPreviewQuery
        {
            BatchCode         = batchCode,
            ProviderProfileId = providerProfileId,
            CommercialModel   = commercialModel,
        }, ct);

        return Ok(result);
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    /// <summary>
    /// POST /api/v1/cargodry/admin/inventory/allocate
    /// Allocates a batch and all its available kits to the given provider.
    /// </summary>
    [HttpPost("allocate")]
    public async Task<IActionResult> AllocateBatch(
        [FromBody] AllocateBatchToProviderRequest request,
        CancellationToken ct)
    {
        var result = await _sender.Send(new AllocateBatchToProviderCommand
        {
            BatchCode              = request.BatchCode,
            ProviderProfileId      = request.ProviderProfileId,
            CommercialModel        = request.CommercialModel,
            SalesChannel           = request.SalesChannel,
            ConsignmentAgreementId = request.ConsignmentAgreementId,
            WarehouseId            = request.WarehouseId,
            Note                   = request.Note,
        }, ct);

        return Ok(result);
    }

    /// <summary>
    /// POST /api/v1/cargodry/admin/inventory/adjust
    /// Manual stock adjustment for an existing provider inventory record.
    /// Positive quantity increases available stock; negative decreases it.
    /// </summary>
    [HttpPost("adjust")]
    public async Task<IActionResult> AdjustInventory(
        [FromBody] AdjustInventoryRequest request,
        CancellationToken ct)
    {
        var result = await _sender.Send(new AdjustProviderInventoryCommand
        {
            ProviderProfileId  = request.ProviderProfileId,
            ProductCode        = request.ProductCode,
            BatchCode          = request.BatchCode,
            AdjustmentQuantity = request.AdjustmentQuantity,
            Reason             = request.Reason,
        }, ct);

        return Ok(result);
    }
}

// ─── Request models ──────────────────────────────────────────────────────────

public sealed class AllocateBatchToProviderRequest
{
    public string                  BatchCode              { get; init; } = default!;
    public long                    ProviderProfileId      { get; init; }
    public CargoDryCommercialModel CommercialModel        { get; init; }
    public SalesChannel            SalesChannel           { get; init; }
    public long?                   ConsignmentAgreementId { get; init; }
    public long?                   WarehouseId            { get; init; }
    public string?                 Note                   { get; init; }
}

public sealed class AdjustInventoryRequest
{
    public long    ProviderProfileId  { get; init; }
    public string  ProductCode        { get; init; } = default!;
    public string? BatchCode          { get; init; }
    public int     AdjustmentQuantity { get; init; }
    public string  Reason             { get; init; } = default!;
}
