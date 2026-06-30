namespace Aizen.Modules.Payment.Abstraction.Requests;

/// <summary>
/// Admin API request: manually confirms gateway payment capture.
/// Used in manual gateway mode — admin confirms bank transfer / EFT received.
/// </summary>
public sealed class CapturePaymentRequest
{
    public required string  GatewayReference { get; init; }
    public required decimal PaidAmount       { get; init; }
    public required string  CurrencyCode     { get; init; }
}
