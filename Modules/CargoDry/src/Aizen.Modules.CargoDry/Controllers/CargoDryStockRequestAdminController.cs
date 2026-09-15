using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Application.Commands.ApproveProviderStockRequest;
using Aizen.Modules.CargoDry.Application.Commands.RejectProviderStockRequest;
using Aizen.Modules.CargoDry.Application.Commands.ShipProviderStockRequest;
using Aizen.Modules.CargoDry.Application.Queries.GetAdminStockRequests;
using Aizen.Modules.CargoDry.Application.Queries.GetAdminStockRequestDetail;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.CargoDry.Controllers;

/// <summary>
/// Admin management of provider stock requests (Wave 4A lifecycle revival): list + approve (allocate) / ship / reject.
/// Approve reuses the canonical AllocateBatchToProvider command. The acting-admin id is asserted in the request body
/// by the AdminPanel BFF (resolved from the verified admin identity), consistent with other decision endpoints.
/// </summary>
[ApiController]
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/v1/cargodry/admin/stock-requests")]
public sealed class CargoDryStockRequestAdminController : ControllerBase
{
    private readonly ISender _sender;

    public CargoDryStockRequestAdminController(ISender sender) => _sender = sender;

    /// <summary>GET /api/v1/cargodry/admin/stock-requests?status=&amp;providerProfileId=&amp;page=&amp;pageSize=</summary>
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] CargoDryStockRequestStatus? status = null,
        [FromQuery] long? providerProfileId = null,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetAdminStockRequestsQuery
        {
            Status = status, ProviderProfileId = providerProfileId, Page = page, PageSize = pageSize,
        }, ct);
        return Ok(result);
    }

    /// <summary>GET /api/v1/cargodry/admin/stock-requests/{id} — single stock request.</summary>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById([FromRoute] long id, CancellationToken ct)
        => Ok(await _sender.Send(new GetAdminStockRequestDetailQuery { RequestId = id }, ct));

    /// <summary>POST /api/v1/cargodry/admin/stock-requests/{id}/approve — allocate a batch + mark Approved.</summary>
    [HttpPost("{id:long}/approve")]
    public async Task<IActionResult> Approve([FromRoute] long id, [FromBody] ApproveStockRequestBody body, CancellationToken ct)
    {
        var result = await _sender.Send(new ApproveProviderStockRequestCommand
        {
            RequestId              = id,
            DecidedByUserId        = body.DecidedByUserId,
            DecisionNote           = body.DecisionNote,
            BatchCode              = body.BatchCode,
            CommercialModel        = body.CommercialModel,
            SalesChannel           = body.SalesChannel,
            ConsignmentAgreementId = body.ConsignmentAgreementId,
            WarehouseId            = body.WarehouseId,
        }, ct);
        return Ok(result);
    }

    /// <summary>POST /api/v1/cargodry/admin/stock-requests/{id}/ship — mark Shipped with a tracking code.</summary>
    [HttpPost("{id:long}/ship")]
    public async Task<IActionResult> Ship([FromRoute] long id, [FromBody] ShipStockRequestBody body, CancellationToken ct)
    {
        var result = await _sender.Send(new ShipProviderStockRequestCommand
        {
            RequestId = id, ShippedByUserId = body.ShippedByUserId, TrackingCode = body.TrackingCode,
        }, ct);
        return Ok(result);
    }

    /// <summary>POST /api/v1/cargodry/admin/stock-requests/{id}/reject — reject with a verbatim reason.</summary>
    [HttpPost("{id:long}/reject")]
    public async Task<IActionResult> Reject([FromRoute] long id, [FromBody] RejectStockRequestBody body, CancellationToken ct)
    {
        var result = await _sender.Send(new RejectProviderStockRequestCommand
        {
            RequestId = id, DecidedByUserId = body.DecidedByUserId, Reason = body.Reason,
        }, ct);
        return Ok(result);
    }

    public sealed class ApproveStockRequestBody
    {
        public long                    DecidedByUserId        { get; init; }
        public string?                 DecisionNote           { get; init; }
        public string                 BatchCode              { get; init; } = default!;
        public CargoDryCommercialModel CommercialModel        { get; init; } = CargoDryCommercialModel.PrincipalSale;
        public SalesChannel            SalesChannel           { get; init; } = SalesChannel.ConsignmentSellThrough;
        public long?                   ConsignmentAgreementId { get; init; }
        public long?                   WarehouseId            { get; init; }
    }

    public sealed class ShipStockRequestBody
    {
        public long   ShippedByUserId { get; init; }
        public string TrackingCode    { get; init; } = default!;
    }

    public sealed class RejectStockRequestBody
    {
        public long   DecidedByUserId { get; init; }
        public string Reason          { get; init; } = default!;
    }
}
