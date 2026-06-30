namespace Aizen.Modules.Payment.Application.Gateway.Apple;

/// <summary>
/// Placeholder implementation of IAppleAppStoreGatewayClient for Phase 2B.
/// Throws NotImplementedException — iOS IAP validation is implemented in Phase 2C.
///
/// Replace this registration in DependencyInjection.cs with the real implementation
/// when Phase 2C is ready:
///   services.AddHttpClient&lt;IAppleAppStoreGatewayClient, AppleAppStoreGatewayClient&gt;(...)
/// </summary>
public sealed class AppleAppStoreGatewayNotImplementedStub : IAppleAppStoreGatewayClient
{
    public Task<AppStoreValidationResult> ValidateReceiptAsync(
        string receiptDataBase64, CancellationToken ct = default)
        => throw new NotImplementedException(
            "Apple App Store IAP validation is not implemented in Phase 2B. " +
            "Implement AppleAppStoreGatewayClient in Phase 2C.");
}
