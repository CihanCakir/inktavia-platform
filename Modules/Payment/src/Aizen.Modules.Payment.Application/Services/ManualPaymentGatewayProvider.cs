using Aizen.Modules.Payment.Abstraction.Interface;
using Aizen.Modules.Payment.Abstraction.Model;
using Aizen.Modules.Payment.Abstraction.Request;
using Aizen.Modules.Payment.Abstraction.Response;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Services;

/// <summary>
/// MVP gateway: all payment operations are admin-confirmed, no real money moves.
/// Architecture is fully Iyzico-ready — swap by changing PAYMENT_GATEWAY_ACTIVE to "iyzico".
/// </summary>
public sealed class ManualPaymentGatewayProvider : IPaymentGatewayProvider
{
    private readonly ILogger<ManualPaymentGatewayProvider> _logger;
    public string ProviderKey => "manual";

    public ManualPaymentGatewayProvider(ILogger<ManualPaymentGatewayProvider> logger)
        => _logger = logger;

    public Task<CheckoutInitResult> InitiateCheckoutAsync(CheckoutInitInput input, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[Manual] Checkout initiated. IdempotencyKey={Key} Amount={Amount} {Currency}",
            input.IdempotencyKey, input.GrossAmount, input.CurrencyCode);

        var result = new CheckoutInitResult
        {
            GatewayReference    = $"MANUAL-{input.IdempotencyKey}",
            CheckoutFormContent = null,
            RedirectUrl         = null,
            IsSuccess           = true,
            ErrorMessage        = null,
        };
        return Task.FromResult(result);
    }

    public Task<PaymentApplyResult> HandleWebhookAsync(ProviderWebhookInput input, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[Manual] Webhook received. GatewayReference={Ref}",
            input.GatewayReference);

        var result = new PaymentApplyResult
        {
            GatewayReference = input.GatewayReference,
            IsSuccess        = true,
            ErrorMessage     = null,
            PaidAmount       = 0m,
            CurrencyCode     = "TRY",
        };
        return Task.FromResult(result);
    }

    public Task<PayoutResult> ReleaseEscrowAsync(ReleaseEscrowInput input, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[Manual] Escrow released. TransactionId={Id} ProviderNet={Net}",
            input.TransactionId, input.ProviderNetAmount);

        var result = new PayoutResult
        {
            Processed       = true,
            GatewayPayoutId = $"PAYOUT-MANUAL-{input.TransactionId}",
            Note            = input.AdminNote ?? "Manual escrow release",
        };
        return Task.FromResult(result);
    }

    public Task<RefundResult> RefundAsync(RefundInput input, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[Manual] Refund processed. TransactionId={Id} Amount={Amount}",
            input.TransactionId, input.RefundAmount);

        var result = new RefundResult
        {
            Processed              = true,
            GatewayRefundReference = $"REFUND-MANUAL-{input.TransactionId}",
            RefundedAmount         = input.RefundAmount,
        };
        return Task.FromResult(result);
    }
}
