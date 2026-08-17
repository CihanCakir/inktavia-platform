namespace Aizen.Modules.Payment.Application.Gateway.Google;

/// <summary>
/// Placeholder implementation of IGooglePlayGatewayClient for Phase 2B.
/// Throws NotImplementedException — Google Play IAP validation is implemented in Phase 2C.
///
/// Replace this registration in DependencyInjection.cs with the real implementation
/// when Phase 2C is ready:
///   services.AddHttpClient&lt;IGooglePlayGatewayClient, GooglePlayGatewayClient&gt;(...)
/// </summary>
public sealed class GooglePlayGatewayNotImplementedStub : IGooglePlayGatewayClient
{
    public Task<PlayStoreValidationResult> ValidatePurchaseTokenAsync(
        string packageName, string subscriptionId, string purchaseToken, CancellationToken ct = default)
        => throw new NotImplementedException(
            "Google Play IAP validation is not implemented in Phase 2B. " +
            "Implement GooglePlayGatewayClient in Phase 2C.");
}
