namespace Aizen.Modules.Payment.Application.Gateway.Apple;

/// <summary>
/// Validates App Store receipts and subscription status with Apple's server-to-server API.
///
/// ── Phase 2C implementation target ───────────────────────────────────────────
///  Production: POST https://buy.itunes.apple.com/verifyReceipt
///  Sandbox:    POST https://sandbox.itunes.apple.com/verifyReceipt
///  Auth:       App-specific shared secret in request body
///
/// ── Flow ─────────────────────────────────────────────────────────────────────
///  1. Mobile app completes StoreKit purchase → gets receipt data (base64)
///  2. App sends receipt to BFF → BFF sends to Payment module
///  3. Payment module calls ValidateReceiptAsync
///  4. On success → ParticipantSubscriptionPaymentSucceededConsumer activates subscription
///
/// Registration: DI registers AppleAppStoreGatewayNotImplementedStub until Phase 2C.
/// </summary>
public interface IAppleAppStoreGatewayClient
{
    /// <summary>
    /// Validates a base64-encoded App Store receipt.
    /// Returns subscription product ID, original transaction ID, and expiry date if valid.
    /// </summary>
    Task<AppStoreValidationResult> ValidateReceiptAsync(
        string receiptDataBase64,
        CancellationToken ct = default);
}

/// <summary>
/// Result of Apple App Store receipt validation.
/// </summary>
public sealed record AppStoreValidationResult(
    bool      IsValid,
    string?   ProductId,
    string?   OriginalTransactionId,
    DateTime? ExpiresDateUtc,
    string?   FailureReason);
