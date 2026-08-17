namespace Aizen.Modules.Payment.Abstraction.Model;

/// <summary>
/// Returned by IPaymentGatewayProvider.InitiateCheckoutAsync.
/// For ManualPaymentGatewayProvider: IsSuccess=true, no redirect URL.
/// For Iyzico: CheckoutFormContent carries the payment form token or HTML.
/// </summary>
public sealed class CheckoutInitResult
{
    /// <summary>Gateway-specific reference stored on PaymentTransactionEntity.GatewayReference.</summary>
    public required string GatewayReference { get; init; }

    /// <summary>
    /// Iyzico checkoutFormContent (token or HTML snippet to embed).
    /// Null for ManualPaymentGatewayProvider.
    /// </summary>
    public string? CheckoutFormContent { get; init; }

    /// <summary>
    /// Redirect URL for browser-based 3D-secure flow.
    /// Null when payment is captured server-side (manual) or uses embedded form.
    /// </summary>
    public string? RedirectUrl { get; init; }

    public required bool   IsSuccess    { get; init; }
    public string?         ErrorMessage { get; init; }
}
