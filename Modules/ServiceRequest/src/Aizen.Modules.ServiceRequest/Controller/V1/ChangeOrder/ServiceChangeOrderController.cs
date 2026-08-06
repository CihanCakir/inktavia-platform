using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ServiceRequest.Abstraction.Request.ChangeOrder;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ChangeOrder;
using Aizen.Modules.ServiceRequest.Application.Command.ChangeOrder;
using Aizen.Modules.ServiceRequest.Application.Query.ChangeOrder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ServiceRequest.Controller.V1.ChangeOrder;

/// <summary>
/// BE-S11b — post-acceptance change orders (§20.13). Provider proposes; customer approves/rejects (exercisable via API/bus
/// without an owner app). Approval applies the incremental economics (a new snapshot + escrow for an increase, or a P10
/// refund for a decrease) — the accepted offer's snapshot/escrow/8-equality is never mutated.
/// </summary>
[ApiController]
[Route("api/v1/service-requests/{serviceRequestId:long}/change-orders")]
[Tags("ServiceRequest - Change Orders")]
[Authorize]
[DocumentationInfo("Change order endpoints", "Post-acceptance change-order lifecycle for service requests.")]
public sealed class ServiceChangeOrderController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ServiceChangeOrderController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    /// <summary>Provider proposes a change order (extra/removed work). Nothing financial until the customer approves.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ServiceChangeOrderDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ServiceChangeOrderDto?>> Propose(
        [FromRoute] long serviceRequestId, [FromBody] ProposeServiceChangeOrderRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ServiceChangeOrderDto>(new ProposeServiceChangeOrderCommand(serviceRequestId, req), ct);
        return SetResponse(result);
    }

    /// <summary>Customer approves → the change order is applied (incremental snapshot + escrow, or a P10 refund). Idempotent.</summary>
    [HttpPatch("{changeOrderId:long}/approve")]
    [ProducesResponseType(typeof(ServiceChangeOrderDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ServiceChangeOrderDto?>> Approve(
        [FromRoute] long serviceRequestId, [FromRoute] long changeOrderId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ServiceChangeOrderDto>(new ApproveServiceChangeOrderCommand(serviceRequestId, changeOrderId), ct);
        return SetResponse(result);
    }

    /// <summary>Customer rejects a proposed change order (terminal, no economics).</summary>
    [HttpPatch("{changeOrderId:long}/reject")]
    [ProducesResponseType(typeof(ServiceChangeOrderDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ServiceChangeOrderDto?>> Reject(
        [FromRoute] long serviceRequestId, [FromRoute] long changeOrderId,
        [FromBody] RejectServiceChangeOrderRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ServiceChangeOrderDto>(new RejectServiceChangeOrderCommand(serviceRequestId, changeOrderId, req), ct);
        return SetResponse(result);
    }

    /// <summary>Lists the SR's change orders + the derived effective total (original acceptance + Σ applied change orders).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ServiceChangeOrderListDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ServiceChangeOrderListDto?>> List(
        [FromRoute] long serviceRequestId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ServiceChangeOrderListDto>(new GetServiceChangeOrdersQuery(serviceRequestId), ct);
        return SetResponse(result);
    }
}
