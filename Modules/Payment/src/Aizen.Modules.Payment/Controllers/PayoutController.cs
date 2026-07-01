using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Requests;
using Aizen.Modules.Payment.Application.Commands.ApproveManualPayout;
using Aizen.Modules.Payment.Application.Commands.HoldPayout;
using Aizen.Modules.Payment.Application.Commands.MarkPayoutComplete;
using Aizen.Modules.Payment.Application.Queries.GetPayoutDetail;
using Aizen.Modules.Payment.Application.Queries.GetPayoutList;
using Aizen.Modules.Payment.Application.Queries.GetPayoutStats;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Payment.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/payment/payouts")]
public sealed class PayoutController : ControllerBase
{
    private readonly ISender _sender;
    public PayoutController(ISender sender) => _sender = sender;

    // ── List & detail ─────────────────────────────────────────────────────────

    /// <summary>Returns a paged list of payout records with optional filters.</summary>
    [HttpGet]
    public async Task<IActionResult> GetPayouts(
        [FromQuery] PayoutStatus? status,
        [FromQuery] long?         providerId,
        [FromQuery] DateTime?     fromDate,
        [FromQuery] DateTime?     toDate,
        [FromQuery] int           page     = 1,
        [FromQuery] int           pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetPayoutListQuery
        {
            Status     = status,
            ProviderId = providerId,
            FromDate   = fromDate,
            ToDate     = toDate,
            Page       = page,
            PageSize   = pageSize,
        }, ct);
        return Ok(result);
    }

    /// <summary>Returns full detail of a single payout record.</summary>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetPayoutDetailQuery { PayoutRecordId = id }, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Returns KPI stats for the payout admin dashboard strip.</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var result = await _sender.Send(new GetPayoutStatsQuery(), ct);
        return Ok(result);
    }

    // ── Legacy / pending list ─────────────────────────────────────────────────

    /// <summary>Returns all Pending payout records (legacy BFF-facing endpoint).</summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending(CancellationToken ct)
    {
        var result = await _sender.Send(new GetPayoutListQuery
        {
            Status   = PayoutStatus.Pending,
            Page     = 1,
            PageSize = 500,
        }, ct);
        return Ok(result);
    }

    // ── State transitions ─────────────────────────────────────────────────────

    /// <summary>Admin marks a payout as completed with the external gateway payout ID.</summary>
    [HttpPost("{id:long}/complete")]
    public async Task<IActionResult> MarkComplete(
        long id, [FromBody] MarkPayoutCompleteRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new MarkPayoutCompleteCommand
        {
            PayoutRecordId  = id,
            GatewayPayoutId = request.GatewayPayoutId,
            AdminNote       = request.AdminNote,
        }, ct);
        return Ok(result);
    }

    /// <summary>Admin places a Pending or Processing payout on hold for compliance review.</summary>
    [HttpPost("{id:long}/hold")]
    public async Task<IActionResult> Hold(
        long id, [FromBody] HoldPayoutRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new HoldPayoutCommand
        {
            PayoutRecordId = id,
            HoldReason     = request.Reason,
            AdminNote      = request.AdminNote,
        }, ct);
        return Ok(result);
    }

    /// <summary>Admin manually approves an OnHold payout with the external bank transfer reference.</summary>
    [HttpPost("{id:long}/approve-manual")]
    public async Task<IActionResult> ApproveManual(
        long id, [FromBody] ApproveManualPayoutRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new ApproveManualPayoutCommand
        {
            PayoutRecordId  = id,
            GatewayPayoutId = request.GatewayPayoutId,
            AdminNote       = request.AdminNote,
        }, ct);
        return Ok(result);
    }
}
