using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Queries.GetPaymentTransaction;
using Aizen.Modules.Payment.Application.Queries.GetPaymentTransactionList;
using Aizen.Modules.Payment.Application.Queries.GetTransactionRefundHistory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Payment.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/payment/transactions")]
public sealed class PaymentTransactionController : ControllerBase
{
    private readonly ISender _sender;
    public PaymentTransactionController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] PaymentTransactionStatus? status,
        [FromQuery] TransactionType?          type,
        [FromQuery] string?                   gateway,
        [FromQuery] DateTime?                 fromDate,
        [FromQuery] DateTime?                 toDate,
        [FromQuery] string?                   search,
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetPaymentTransactionListQuery
        {
            Status = status, Type = type, Gateway = gateway,
            FromDate = fromDate, ToDate = toDate, Search = search,
            Page = page, PageSize = pageSize,
        }, ct);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetPaymentTransactionQuery { TransactionId = id }, ct);
        if (result is null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Returns all refund records for a transaction, newest first.
    /// Includes status, amounts, gateway references, and reversal details.
    /// </summary>
    [HttpGet("{id:long}/refund-history")]
    public async Task<IActionResult> GetRefundHistory(long id, CancellationToken ct)
    {
        var result = await _sender.Send(
            new GetTransactionRefundHistoryQuery { TransactionId = id }, ct);
        return Ok(result);
    }
}
