using Aizen.Core.CQRS.Handler;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Payment.Abstraction.Request;
using Aizen.Modules.Payment.Abstraction.Response;
using Aizen.Modules.Payment.Application.Gateway.Iyzico;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.Extensions.Logging;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.Payment.Application.Commands.ProcessIyzicoWebhook;

/// <summary>
/// Handles the full Iyzico webhook lifecycle:
///   1. Validate incoming token field
///   2. Retrieve payment result from Iyzico gateway
///   3. Capture the transaction directly (inline — no nested command dispatch)
///
/// Exceptions are intentionally caught and logged — never re-thrown.
/// Webhook handlers must not throw: Iyzico retries on non-2xx responses,
/// which can cause duplicate capture attempts.
/// </summary>
[DocumentationInfo("Process Iyzico webhook command handler",
    "Orchestrates Iyzico payment webhook: validates token, retrieves result from Iyzico, captures transaction inline. Never throws — all errors are logged and returned as a failure result.")]
public sealed class ProcessIyzicoWebhookCommandHandler
    : AizenCommandHandler<ProcessIyzicoWebhookCommand, ProcessIyzicoWebhookResult>
{
    private readonly IyzicoMarketplacePaymentGatewayProvider   _iyzicoProvider;
    private readonly IPaymentTransactionRepository             _transactions;
    private readonly ILogger<ProcessIyzicoWebhookCommandHandler> _logger;

    public ProcessIyzicoWebhookCommandHandler(
        IAizenUnitOfWork<PaymentDbContext>               unitOfWork,
        IyzicoMarketplacePaymentGatewayProvider          iyzicoProvider,
        IPaymentTransactionRepository                    transactions,
        ILogger<ProcessIyzicoWebhookCommandHandler>      logger)
    {
        _iyzicoProvider = iyzicoProvider;
        _transactions   = transactions;
        _logger         = logger;
    }

    public override async Task<ProcessIyzicoWebhookResult?> Handle(
        ProcessIyzicoWebhookCommand request, CancellationToken ct)
    {
        _logger.LogInformation(
            "Iyzico webhook received. Token={Token} Status={Status}",
            request.Token, request.Status);

        if (string.IsNullOrWhiteSpace(request.Token))
        {
            _logger.LogWarning("Iyzico webhook missing token field — ignoring.");
            return new ProcessIyzicoWebhookResult { Processed = false, FailureReason = "missing_token" };
        }

        var rawBody = $"token={request.Token}&status={request.Status}";

        var webhookInput = new ProviderWebhookInput(
            GatewayReference: request.Token,
            RawBody:          rawBody,
            Signature:        request.Signature,
            Headers:          request.Headers);

        PaymentApplyResult gatewayResult;
        try
        {
            gatewayResult = await _iyzicoProvider.HandleWebhookAsync(webhookInput, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in Iyzico gateway handler. Token={Token}", request.Token);
            return new ProcessIyzicoWebhookResult { Processed = false, FailureReason = "gateway_exception" };
        }

        if (!gatewayResult.IsSuccess)
        {
            _logger.LogWarning(
                "Iyzico payment not successful. Token={Token} Error={Error}",
                request.Token, gatewayResult.ErrorMessage);
            return new ProcessIyzicoWebhookResult { Processed = false, FailureReason = gatewayResult.ErrorMessage };
        }

        // ── Inline capture logic (must not dispatch nested commands) ────────────
        try
        {
            var tx = await _transactions.GetByGatewayReferenceAsync(gatewayResult.GatewayReference, ct);

            if (tx is null)
            {
                _logger.LogWarning(
                    "CapturePayment: transaction not found for GatewayReference={Ref}. Token={Token}",
                    gatewayResult.GatewayReference, request.Token);
                return new ProcessIyzicoWebhookResult { Processed = false, FailureReason = "transaction_not_found" };
            }

            // Idempotency — duplicate webhook: skip re-capture
            if (tx.CapturedAt.HasValue)
            {
                _logger.LogInformation(
                    "CapturePayment: transaction {Id} already captured at {At}. Idempotent webhook.",
                    tx.Id, tx.CapturedAt);
                return new ProcessIyzicoWebhookResult { Processed = true };
            }

            tx.Capture(gatewayResult.GatewayReference);
            _transactions.Update(tx);
            // SaveChanges is handled by AizenCommandHandlerDecorator — do NOT call here.

            _logger.LogInformation(
                "Payment captured via webhook. TransactionId={Id} GatewayRef={Ref} Amount={Amount}",
                tx.Id, gatewayResult.GatewayReference, gatewayResult.PaidAmount);
        }
        catch (Exception ex)
        {
            // Payment reference confirmed by Iyzico — log for dead-letter retry (post-MVP).
            _logger.LogError(ex,
                "Capture failed after Iyzico webhook confirmation. Token={Token}", request.Token);
            return new ProcessIyzicoWebhookResult { Processed = false, FailureReason = "capture_failed" };
        }

        return new ProcessIyzicoWebhookResult { Processed = true };
    }
}
