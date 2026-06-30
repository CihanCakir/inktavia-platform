namespace Aizen.Modules.Payment.Abstraction.Request;

/// <summary>
/// Input carried from the webhook controller into IPaymentGatewayProvider.HandleWebhookAsync.
/// For Iyzico: GatewayReference = checkoutToken from the webhook form body.
/// The provider uses this token to call Iyzico's retrieve endpoint and confirm payment.
/// </summary>
public sealed record ProviderWebhookInput(
    /// <summary>Iyzico checkoutToken (or manual gateway reference). Used to retrieve payment result.</summary>
    string GatewayReference,
    /// <summary>Raw HTTP body — stored for audit/replay purposes.</summary>
    string RawBody,
    /// <summary>Iyzico-Signature header value (if present) for HMAC validation.</summary>
    string? Signature,
    /// <summary>All request headers — passed through for gateway-specific validation.</summary>
    IReadOnlyDictionary<string, string>? Headers = null,
    /// <summary>UTC timestamp from webhook payload; null = use DateTime.UtcNow.</summary>
    DateTime? PaidAtUtc = null
);
