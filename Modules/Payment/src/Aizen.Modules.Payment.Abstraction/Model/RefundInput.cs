using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Abstraction.Model;

public sealed class RefundInput
{
    public required long    TransactionId      { get; init; }
    public required string  GatewayReference   { get; init; }
    public required decimal RefundAmount       { get; init; }
    public required string  Currency           { get; init; }
    public          RefundReason Reason        { get; init; } = RefundReason.UserCancel;
    public          string? AdminNote          { get; init; }
}
