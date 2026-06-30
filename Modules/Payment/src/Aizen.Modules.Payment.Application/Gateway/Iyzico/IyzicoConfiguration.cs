namespace Aizen.Modules.Payment.Application.Gateway.Iyzico;

/// <summary>
/// Bound from "Iyzico" configuration section.
/// All secrets are #{placeholder}# in non-local environments.
/// </summary>
public sealed class IyzicoConfiguration
{
    /// <summary>Iyzico merchant API key.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Iyzico merchant secret key (HMAC signing).</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// https://sandbox-api.iyzipay.com  (dev/local)
    /// https://api.iyzipay.com           (production)
    /// </summary>
    public string BaseUrl { get; set; } = "https://sandbox-api.iyzipay.com";

    /// <summary>
    /// Webhook HMAC secret — configured in Iyzico merchant panel.
    /// If empty, webhook HMAC validation is skipped (dev-only behaviour).
    /// </summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>
    /// URL we pass to Iyzico as callbackUrl for the CheckoutForm.
    /// Iyzico redirects/posts here after 3D-secure step.
    /// Example: https://app.inktavia.com/payment/callback
    /// </summary>
    public string CallbackUrl { get; set; } = "https://localhost:3000/payment/callback";

    /// <summary>ISO 639-1 locale sent to Iyzico forms ("tr" | "en").</summary>
    public string Locale { get; set; } = "tr";
}
