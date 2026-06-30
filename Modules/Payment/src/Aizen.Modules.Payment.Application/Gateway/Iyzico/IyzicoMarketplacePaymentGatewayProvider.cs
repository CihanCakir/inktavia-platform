using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Interface;
using Aizen.Modules.Payment.Abstraction.Model;
using Aizen.Modules.Payment.Abstraction.Request;
using Aizen.Modules.Payment.Abstraction.Response;
using Aizen.Modules.Payment.Application.Gateway.Iyzico.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Payment.Application.Gateway.Iyzico;

/// <summary>
/// Iyzico Marketplace payment gateway provider.
/// Implements the three-party marketplace flow:
///   Buyer → Platform (escrow) → Provider sub-merchant (released on SR completion)
///
/// Assumptions (Phase 2 MVP):
/// - Buyer identity data is minimal (profile ID only); full KYC enrichment is post-MVP.
/// - SubMerchantKey must be pre-registered via RegisterSubMerchantCommand before payments.
/// - Webhook HMAC validation is configurable (enabled in production, optional in dev).
/// - PaymentTransactionId from Iyzico (needed for marketplace approval) is stored in
///   IyzicoPaymentItem returned by retrieve; we use the first item's ID for MVP.
/// </summary>
[DocumentationInfo("Iyzico Marketplace gateway provider",
    "Three-party marketplace: buyer→escrow→sub-merchant payout on SR completion.")]
public sealed class IyzicoMarketplacePaymentGatewayProvider : IPaymentGatewayProvider
{
    private readonly IyzicoHttpClient _client;
    private readonly IyzicoConfiguration _config;
    private readonly ILogger<IyzicoMarketplacePaymentGatewayProvider> _logger;

    public string ProviderKey => "iyzico";

    public IyzicoMarketplacePaymentGatewayProvider(
        IyzicoHttpClient client,
        IOptions<IyzicoConfiguration> config,
        ILogger<IyzicoMarketplacePaymentGatewayProvider> logger)
    {
        _client = client;
        _config = config.Value;
        _logger = logger;
    }

    // ── InitiateCheckoutAsync ─────────────────────────────────────────────────

    public async Task<CheckoutInitResult> InitiateCheckoutAsync(
        CheckoutInitInput input, CancellationToken ct = default)
    {
        var grossStr  = FormatAmount(input.GrossAmount);
        var netStr    = FormatAmount(input.ProviderNetAmount);
        var buyerId   = input.PayerProfileId.ToString();
        var basketId  = $"TXN-{input.TransactionId}";

        // Build basket item with sub-merchant split
        var basketItem = new IyzicoBasketItem
        {
            Id              = basketId,
            Name            = input.Description ?? $"Service Request Payment",
            Category1       = "Marine Services",
            ItemType        = "VIRTUAL",
            Price           = grossStr,
            SubMerchantKey  = input.SubMerchantKey,   // null for non-marketplace payments
            SubMerchantPrice = input.SubMerchantKey is not null ? netStr : null,
        };

        // MVP buyer: minimal — post-MVP enrich from Identity module
        var buyer = new IyzicoBuyer
        {
            Id      = buyerId,
            Name    = "Marine",
            Surname = $"User-{buyerId}",
            Email   = $"user{buyerId}@inktavia.com",
            Ip      = "127.0.0.1",
        };

        var address = new IyzicoAddress
        {
            ContactName = $"User {buyerId}",
            City        = "Istanbul",
            Country     = "Turkey",
            Address     = "N/A",
        };

        var checkoutRequest = new IyzicoCheckoutFormRequest
        {
            Locale         = _config.Locale,
            ConversationId = input.IdempotencyKey,
            Price          = grossStr,
            PaidPrice      = grossStr,
            Currency       = input.CurrencyCode,
            BasketId       = basketId,
            PaymentGroup   = "PRODUCT",
            CallbackUrl    = _config.CallbackUrl,
            EnabledInstallments = [1],
            Buyer           = buyer,
            ShippingAddress = address,
            BillingAddress  = address,
            BasketItems     = [basketItem],
        };

        var response = await _client.InitializeCheckoutFormAsync(checkoutRequest, ct);

        if (response is null || !response.IsSuccess)
        {
            var errMsg = response?.ErrorMessage ?? "Iyzico API returned null response.";
            _logger.LogError(
                "Iyzico checkout form init failed. IdempotencyKey={Key} Error={Error} Code={Code}",
                input.IdempotencyKey, errMsg, response?.ErrorCode);

            return new CheckoutInitResult
            {
                GatewayReference    = string.Empty,
                CheckoutFormContent = null,
                RedirectUrl         = null,
                IsSuccess           = false,
                ErrorMessage        = $"[{response?.ErrorCode}] {errMsg}",
            };
        }

        _logger.LogInformation(
            "Iyzico checkout form created. Token={Token} IdempotencyKey={Key}",
            response.Token, input.IdempotencyKey);

        return new CheckoutInitResult
        {
            GatewayReference    = response.Token ?? string.Empty,
            CheckoutFormContent = response.CheckoutFormContent,
            RedirectUrl         = null,  // Iyzico uses embedded form, not redirect
            IsSuccess           = true,
            ErrorMessage        = null,
        };
    }

    // ── HandleWebhookAsync ────────────────────────────────────────────────────

    /// <summary>
    /// Iyzico webhook handler.
    /// The controller extracts the token from the POST body and passes it as GatewayReference.
    /// We retrieve the payment result from Iyzico to confirm payment status.
    /// </summary>
    public async Task<PaymentApplyResult> HandleWebhookAsync(
        ProviderWebhookInput input, CancellationToken ct = default)
    {
        // Validate webhook signature if configured
        if (!string.IsNullOrEmpty(input.Signature))
        {
            if (!_client.ValidateWebhookSignature(input.GatewayReference, input.Signature))
            {
                _logger.LogWarning(
                    "Iyzico webhook HMAC mismatch. Token={Token}", input.GatewayReference);

                return new PaymentApplyResult
                {
                    GatewayReference = input.GatewayReference,
                    IsSuccess        = false,
                    ErrorMessage     = "Webhook HMAC validation failed.",
                };
            }
        }

        // Retrieve the payment form result to confirm status
        var retrieveRequest = new IyzicoRetrieveCheckoutRequest
        {
            Locale         = _config.Locale,
            ConversationId = $"WHK-{input.GatewayReference[..Math.Min(8, input.GatewayReference.Length)]}",
            Token          = input.GatewayReference,
        };

        var response = await _client.RetrieveCheckoutFormAsync(retrieveRequest, ct);

        if (response is null || !response.IsSuccess || response.PaymentStatus != "SUCCESS")
        {
            var errMsg = response?.ErrorMessage ?? "Iyzico retrieve returned null.";
            _logger.LogWarning(
                "Iyzico payment NOT successful. Token={Token} Status={Status} Error={Error}",
                input.GatewayReference, response?.PaymentStatus, errMsg);

            return new PaymentApplyResult
            {
                GatewayReference = input.GatewayReference,
                IsSuccess        = false,
                ErrorMessage     = errMsg,
                PaidAmount       = 0m,
                CurrencyCode     = response?.Currency ?? "TRY",
            };
        }

        var paidAmount = TryParseDecimal(response.PaidPrice);
        // Capture the Iyzico paymentTransactionId for marketplace approval on release
        var iyzicoTxId = response.PaymentItems?.FirstOrDefault()?.PaymentTransactionId;

        _logger.LogInformation(
            "Iyzico payment confirmed. Token={Token} PaidAmount={Amount} IyzicoTxId={IyzicoTxId}",
            input.GatewayReference, paidAmount, iyzicoTxId);

        return new PaymentApplyResult
        {
            GatewayReference     = input.GatewayReference,
            IsSuccess            = true,
            PaidAmount           = paidAmount,
            CurrencyCode         = response.Currency ?? "TRY",
            GatewayTransactionId = iyzicoTxId,
        };
    }

    // ── ReleaseEscrowAsync ────────────────────────────────────────────────────

    /// <summary>
    /// Releases marketplace escrow to the provider sub-merchant via Iyzico's approval API.
    /// The Iyzico paymentTransactionId is expected in ReleaseEscrowInput.GatewayReference
    /// (set when the webhook was processed and CapturePaymentCommand was called).
    ///
    /// Post-MVP: store Iyzico paymentTransactionId separately on the transaction entity.
    /// MVP: use GatewayReference as the Iyzico transaction ID.
    /// </summary>
    public async Task<PayoutResult> ReleaseEscrowAsync(
        ReleaseEscrowInput input, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(input.GatewayReference))
        {
            _logger.LogWarning(
                "Iyzico escrow release skipped — no gateway reference. TransactionId={Id}",
                input.TransactionId);

            return new PayoutResult
            {
                Processed = false,
                Note      = "Missing Iyzico paymentTransactionId — manual payout required.",
            };
        }

        var approvalRequest = new IyzicoApprovalRequest
        {
            Locale                = _config.Locale,
            ConversationId        = $"REL-{input.TransactionId}",
            PaymentTransactionId  = input.GatewayReference,
        };

        var response = await _client.ApproveMarketplacePaymentAsync(approvalRequest, ct);

        if (response is null || !response.IsSuccess)
        {
            var errMsg = response?.ErrorMessage ?? "Iyzico approval returned null.";
            _logger.LogError(
                "Iyzico marketplace approval failed. TransactionId={Id} IyzicoTxId={IyzicoTxId} Error={Error}",
                input.TransactionId, input.GatewayReference, errMsg);

            return new PayoutResult
            {
                Processed       = false,
                GatewayPayoutId = null,
                Note            = $"Iyzico approval failed: [{response?.ErrorCode}] {errMsg}",
            };
        }

        _logger.LogInformation(
            "Iyzico marketplace approval succeeded. TransactionId={Id} IyzicoTxId={IyzicoTxId} ProviderNet={Net}",
            input.TransactionId, input.GatewayReference, input.ProviderNetAmount);

        return new PayoutResult
        {
            Processed       = true,
            GatewayPayoutId = response.PaymentTransactionId ?? input.GatewayReference,
            Note            = input.AdminNote ?? "Iyzico marketplace payout approved.",
        };
    }

    // ── RefundAsync ───────────────────────────────────────────────────────────

    public async Task<RefundResult> RefundAsync(RefundInput input, CancellationToken ct = default)
    {
        var refundRequest = new IyzicoRefundRequest
        {
            Locale               = _config.Locale,
            ConversationId       = $"RFD-{input.TransactionId}",
            PaymentTransactionId = input.GatewayReference,  // Iyzico payment transaction ID
            Price                = FormatAmount(input.RefundAmount),
            Currency             = input.Currency,
        };

        var response = await _client.RefundAsync(refundRequest, ct);

        if (response is null || !response.IsSuccess)
        {
            var errMsg = response?.ErrorMessage ?? "Iyzico refund returned null.";
            _logger.LogError(
                "Iyzico refund failed. TransactionId={Id} RefundAmount={Amount} Error={Error}",
                input.TransactionId, input.RefundAmount, errMsg);

            return new RefundResult
            {
                Processed              = false,
                GatewayRefundReference = null,
                RefundedAmount         = 0m,
            };
        }

        var refundedAmount = TryParseDecimal(response.Price);

        _logger.LogInformation(
            "Iyzico refund succeeded. TransactionId={Id} RefundedAmount={Amount}",
            input.TransactionId, refundedAmount);

        return new RefundResult
        {
            Processed              = true,
            GatewayRefundReference = response.PaymentTransactionId ?? input.GatewayReference,
            RefundedAmount         = refundedAmount,
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Iyzico requires amounts as strings with dot decimal separator, e.g. "100.00".
    /// Always use InvariantCulture — never locale-sensitive formatting.
    /// </summary>
    private static string FormatAmount(decimal amount)
        => amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

    private static decimal TryParseDecimal(string? value)
        => decimal.TryParse(value, System.Globalization.NumberStyles.Any,
               System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : 0m;
}
