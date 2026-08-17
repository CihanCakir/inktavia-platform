namespace Aizen.Modules.Payment.Abstraction.Response;

public sealed class PayoutResult
{
    public bool    Processed          { get; init; }
    public string? GatewayPayoutId    { get; init; }  // Iyzico transfer reference, null for manual
    public string? Note               { get; init; }
}
