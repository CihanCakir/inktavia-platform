namespace Aizen.Modules.Payment.Abstraction.Response;

public sealed class RefundResult
{
    public bool    Processed                { get; init; }
    public string? GatewayRefundReference   { get; init; }
    public decimal RefundedAmount           { get; init; }
}
