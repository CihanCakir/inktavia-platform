using System.Text;
using Aizen.Modules.Payment.Abstraction.Request;
using Aizen.Modules.Payment.Abstraction.Response;
using Aizen.Modules.Payment.Application.Commands.CapturePayment;
using Aizen.Modules.Payment.Application.Gateway.Iyzico;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Controllers;

/// <summary>
/// Receives payment gateway webhooks (currently: Iyzico).
/// All webhook endpoints are [AllowAnonymous] — Iyzico calls these from their server.
/// Security is enforced via HMAC signature validation inside the gateway provider.
///
/// Route: POST /api/v1/payment/webhook/iyzico
///
/// Iyzico webhook payload (form POST):
///   token=xxx&status=SUCCESS (or other fields depending on event type)
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/payment/webhook")]
public sealed class PaymentWebhookController : ControllerBase
{
    private readonly ISender                                        _sender;
    private readonly IyzicoMarketplacePaymentGatewayProvider        _iyzicoProvider;
    private readonly ILogger<PaymentWebhookController>              _logger;

    public PaymentWebhookController(
        ISender sender,
        IyzicoMarketplacePaymentGatewayProvider iyzicoProvider,
        ILogger<PaymentWebhookController> logger)
    {
        _sender         = sender;
        _iyzicoProvider = iyzicoProvider;
        _logger         = logger;
    }

    /// <summary>
    /// Iyzico posts here after payment form completion.
    /// Body: application/x-www-form-urlencoded with "token" field.
    ///
    /// Flow:
    ///   1. Extract token from form body
    ///   2. Validate HMAC signature (if signature header present)
    ///   3. Retrieve payment details from Iyzico
    ///   4. If success → CapturePaymentCommand to move transaction to Captured
    ///   5. Return 200 OK (Iyzico retries on non-2xx)
    /// </summary>
    [HttpPost("iyzico")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> IyzicoWebhook(
        [FromForm] string? token,
        [FromForm] string? status,
        CancellationToken ct)
    {
        // Read raw body for audit log
        var rawBody = $"token={token}&status={status}";

        _logger.LogInformation(
            "Iyzico webhook received. Token={Token} Status={Status}", token, status);

        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Iyzico webhook missing token field.");
            return Ok(); // Return 200 to prevent Iyzico retries for bad requests
        }

        // Extract signature from header (Iyzico-Signature) if present
        var signature = Request.Headers.TryGetValue("x-iyz-signature", out var sig)
            ? sig.ToString()
            : null;

        // Collect all incoming headers for gateway-level inspection
        var headers = Request.Headers
            .ToDictionary(h => h.Key, h => h.Value.ToString());

        var webhookInput = new ProviderWebhookInput(
            GatewayReference: token,
            RawBody:          rawBody,
            Signature:        signature,
            Headers:          headers);

        // Delegate to the Iyzico provider — retrieves payment details from Iyzico API
        PaymentApplyResult result;
        try
        {
            result = await _iyzicoProvider.HandleWebhookAsync(webhookInput, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in Iyzico webhook handler. Token={Token}", token);
            return Ok(); // Don't expose exceptions; prevents Iyzico retry storm
        }

        if (!result.IsSuccess)
        {
            _logger.LogWarning(
                "Iyzico payment not successful. Token={Token} Error={Error}",
                token, result.ErrorMessage);
            return Ok(); // Still 200 — non-success is a business outcome, not an error
        }

        // Capture the transaction in our system
        try
        {
            await _sender.Send(new CapturePaymentCommand
            {
                GatewayReference = result.GatewayReference,
                PaidAmount       = result.PaidAmount,
                CurrencyCode     = result.CurrencyCode,
            }, ct);

            _logger.LogInformation(
                "Payment captured after Iyzico webhook. Token={Token} Amount={Amount}",
                token, result.PaidAmount);
        }
        catch (Exception ex)
        {
            // Log and return 200 — the transaction reference is already confirmed by Iyzico.
            // Post-MVP: dead-letter queue for retry.
            _logger.LogError(ex,
                "CapturePaymentCommand failed after Iyzico webhook. Token={Token}", token);
        }

        return Ok();
    }

    /// <summary>
    /// Manual gateway webhook (admin-triggered, for MVP/testing).
    /// Simulates a payment success notification for a given transaction.
    /// Protected by Admin role since this bypasses real payment flow.
    /// </summary>
    [HttpPost("manual/{transactionId:long}/capture")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> ManualCapture(
        long transactionId,
        [FromQuery] decimal paidAmount = 0m,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Manual payment capture. TransactionId={Id} PaidAmount={Amount}",
            transactionId, paidAmount);

        await _sender.Send(new CapturePaymentCommand
        {
            GatewayReference = $"MANUAL-CAPTURE-{transactionId}",
            PaidAmount       = paidAmount,
            CurrencyCode     = "TRY",
        }, ct);

        return Ok(new { Message = $"Transaction {transactionId} captured.", PaidAmount = paidAmount });
    }
}
