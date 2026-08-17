namespace Aizen.Modules.Payment.Application.Gateway.Google;

/// <summary>
/// Validates Google Play subscription purchases via the Android Publisher API.
///
/// ── Phase 2C implementation target ───────────────────────────────────────────
///  API: GET https://androidpublisher.googleapis.com/androidpublisher/v3/
///           applications/{packageName}/purchases/subscriptions/{subscriptionId}/tokens/{token}
///  Auth: Google Service Account OAuth2 (JWT, server-to-server)
///
/// ── Flow ─────────────────────────────────────────────────────────────────────
///  1. Mobile app completes Play Billing purchase → gets purchaseToken
///  2. App sends token + subscriptionId to BFF → BFF sends to Payment module
///  3. Payment module calls ValidatePurchaseTokenAsync
///  4. On success → ParticipantSubscriptionPaymentSucceededConsumer activates subscription
///
/// Registration: DI registers GooglePlayGatewayNotImplementedStub until Phase 2C.
/// </summary>
public interface IGooglePlayGatewayClient
{
    /// <summary>
    /// Validates a Google Play purchase token.
    /// Returns order ID and subscription expiry date if valid.
    /// </summary>
    Task<PlayStoreValidationResult> ValidatePurchaseTokenAsync(
        string packageName,
        string subscriptionId,
        string purchaseToken,
        CancellationToken ct = default);
}

/// <summary>
/// Result of Google Play purchase token validation.
/// </summary>
public sealed record PlayStoreValidationResult(
    bool      IsValid,
    string?   OrderId,
    DateTime? ExpiresDateUtc,
    string?   FailureReason);
