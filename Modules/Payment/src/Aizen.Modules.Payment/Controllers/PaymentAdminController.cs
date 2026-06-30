using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Requests;
using Aizen.Modules.Payment.Application.Commands.CancelPayment;
using Aizen.Modules.Payment.Application.Commands.CapturePayment;
using Aizen.Modules.Payment.Application.Commands.CreatePaymentEscrow;
using Aizen.Modules.Payment.Application.Commands.RefundPayment;
using Aizen.Modules.Payment.Application.Commands.ReinstateCancelledTransaction;
using Aizen.Modules.Payment.Application.Commands.ReversePartialRefund;
using Aizen.Modules.Payment.Application.Queries.GetTransactionRefundHistory;
using Aizen.Modules.Payment.Application.Commands.ReleasePaymentEscrow;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Payment.Controllers;

[ApiController]
[Authorize(Roles = "payment.admin")]
[Route("api/v1/payment/admin")]
public sealed class PaymentAdminController : ControllerBase
{
    private readonly ISender _sender;
    public PaymentAdminController(ISender sender) => _sender = sender;

    // ── Escrow lifecycle ──────────────────────────────────────────────────────

    /// <summary>Creates a new escrow transaction. Called from ServiceRequest module after offer acceptance.</summary>
    [HttpPost("escrow")]
    public async Task<IActionResult> CreateEscrow(
        [FromBody] CreateEscrowRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new CreatePaymentEscrowCommand
        {
            IdempotencyKey     = request.IdempotencyKey,
            Context            = request.Context,
            TransactionType    = request.TransactionType,
            PayerProfileId     = request.PayerProfileId,
            RecipientProfileId = request.RecipientProfileId,
            GrossAmount        = request.GrossAmount,
            DiscountAmount     = request.DiscountAmount,
            CurrencyCode       = request.CurrencyCode,
            ProviderPlanId     = request.ProviderPlanId,
            CategoryCode       = request.CategoryCode,
            EscrowRequired     = request.EscrowRequired,
        }, ct);
        return Ok(result);
    }

    /// <summary>Confirms payment capture (gateway webhook confirmation for manual gateway).</summary>
    [HttpPost("transactions/{id:long}/capture")]
    public async Task<IActionResult> Capture(
        long id, [FromBody] CapturePaymentRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new CapturePaymentCommand
        {
            TransactionId    = id,
            GatewayReference = request.GatewayReference,
            PaidAmount       = request.PaidAmount,
            CurrencyCode     = request.CurrencyCode,
        }, ct);
        return Ok(result);
    }

    /// <summary>Releases escrow after SR completion approval. Creates a PayoutRecord.</summary>
    [HttpPost("transactions/{id:long}/release")]
    public async Task<IActionResult> ReleaseEscrow(
        long id, [FromBody] ReleaseEscrowRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new ReleasePaymentEscrowCommand
        {
            TransactionId    = id,
            ApprovedByUserId = request.ApprovedByUserId,
            AdminNote        = request.AdminNote,
        }, ct);
        return Ok(result);
    }

    // ── Refund operations ─────────────────────────────────────────────────────

    /// <summary>
    /// Issues a full or partial gateway refund for a Captured/Released/PartiallyRefunded transaction.
    /// Creates a TransactionRefundRecord and updates TotalRefundedAmount on the transaction.
    /// </summary>
    [HttpPost("transactions/{id:long}/refund")]
    public async Task<IActionResult> Refund(
        long id, [FromBody] RefundPaymentRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new RefundPaymentCommand
        {
            TransactionId = id,
            RefundAmount  = request.RefundAmount,
            Reason        = request.Reason,
            RefundType    = request.RefundType,
            AdminNote     = request.AdminNote,
        }, ct);
        return Ok(result);
    }

    /// <summary>
    /// Reverses a previously processed refund record (e.g., issued in error before bank settlement).
    /// The RefundRecord is marked Reversed — not deleted. TotalRefundedAmount is recalculated.
    /// </summary>
    [HttpPost("refund-records/{refundRecordId:long}/reverse")]
    public async Task<IActionResult> ReverseRefund(
        long refundRecordId, [FromBody] ReverseRefundRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new ReversePartialRefundCommand
        {
            RefundRecordId = refundRecordId,
            ReversalReason = request.ReversalReason,
            AdminNote      = request.AdminNote,
        }, ct);
        return Ok(result);
    }

    /// <summary>Returns the full refund history (all TransactionRefundRecords) for a transaction.</summary>
    [HttpGet("transactions/{id:long}/refund-history")]
    public async Task<IActionResult> GetRefundHistory(long id, CancellationToken ct)
    {
        var result = await _sender.Send(
            new GetTransactionRefundHistoryQuery { TransactionId = id }, ct);
        return Ok(result);
    }

    // ── Cancellation operations ───────────────────────────────────────────────

    /// <summary>
    /// Cancels a PendingIntent transaction before any money is captured.
    /// No gateway call needed — intent is voided in-system.
    /// </summary>
    [HttpPost("transactions/{id:long}/cancel")]
    public async Task<IActionResult> Cancel(
        long id, [FromBody] CancelPaymentRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new CancelPaymentCommand
        {
            TransactionId      = id,
            CancellationReason = request.CancellationReason,
            AdminNote          = request.AdminNote,
        }, ct);
        return Ok(result);
    }

    /// <summary>
    /// Reinstates a previously cancelled transaction back to PendingIntent.
    /// Use when a cancellation was made in error and the payer needs to retry.
    /// </summary>
    [HttpPost("transactions/{id:long}/reinstate")]
    public async Task<IActionResult> Reinstate(
        long id, [FromBody] ReinstatePaymentRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new ReinstateCancelledTransactionCommand
        {
            TransactionId = id,
            AdminNote     = request.AdminNote,
        }, ct);
        return Ok(result);
    }
}

