using Aizen.Modules.Payment.Application.Commands.CapturePayment;
using Aizen.Modules.Payment.Application.Commands.ProcessIyzicoWebhook;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Payment.Controllers;

/// <summary>
/// Receives payment gateway webhooks (currently: Iyzico).
/// All webhook endpoints are [AllowAnonymous] — Iyzico calls these from their server.
/// Security is enforced via HMAC signature validation inside the gateway provider.
///
/// Important: webhook endpoints MUST always return HTTP 200.
/// Iyzico retries delivery on non-2xx responses, which can cause duplicate capture attempts.
/// All business logic and error handling lives in ProcessIyzicoWebhookCommandHandler.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/payment/webhook")]
public sealed class PaymentWebhookController : ControllerBase
{
    private readonly ISender _sender;

    public PaymentWebhookController(ISender sender) => _sender = sender;

    /// <summary>
    /// Iyzico posts here after payment form completion (form-POST with token field).
    /// Always returns 200 OK — error handling is inside the command handler.
    /// </summary>
    [HttpPost("iyzico")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> IyzicoWebhook(
        [FromForm] string? token,
        [FromForm] string? status,
        [FromForm] string? iyziEventType,
        [FromForm] string? iyziPaymentId,
        [FromForm] string? paymentConversationId,
        [FromForm] string? iyziReferenceCode,
        CancellationToken ct)
    {
        // BE-P9-fix §2: the CheckoutForm webhook signature is in X-IYZ-SIGNATURE-V3 (V1/V2 deprecated).
        var signature = Request.Headers.TryGetValue("X-IYZ-SIGNATURE-V3", out var v3) ? v3.ToString()
                      : Request.Headers.TryGetValue("x-iyz-signature", out var sig) ? sig.ToString()
                      : null;

        var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());

        await _sender.Send(new ProcessIyzicoWebhookCommand
        {
            Token                 = token,
            Status                = status,
            Signature             = signature,
            IyziEventType         = iyziEventType,
            IyziPaymentId         = iyziPaymentId,
            PaymentConversationId = paymentConversationId,
            IyziReferenceCode     = iyziReferenceCode,
            Headers               = headers,
        }, ct);

        return Ok();
    }

    /// <summary>
    /// Manual gateway capture (admin-triggered, for MVP/testing).
    /// Bypasses real payment flow — protected by Admin role.
    /// TransactionId is passed explicitly so the handler resolves via PK (fastest path).
    /// GatewayReference is set to a deterministic manual key for audit trail purposes.
    /// </summary>
    [HttpPost("manual/{transactionId:long}/capture")]
    [Authorize(Roles = "payment.admin")]
    public async Task<IActionResult> ManualCapture(
        long transactionId,
        [FromQuery] decimal paidAmount = 0m,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new CapturePaymentCommand
        {
            TransactionId    = transactionId,
            GatewayReference = $"MANUAL-CAPTURE-{transactionId}",
            PaidAmount       = paidAmount,
            CurrencyCode     = "TRY",
        }, ct);

        return Ok(new
        {
            Message            = $"Transaction {transactionId} captured.",
            TransactionCode    = result?.TransactionCode,
            WasAlreadyCaptured = result?.WasAlreadyCaptured,
            PaidAmount         = paidAmount,
        });
    }
}
