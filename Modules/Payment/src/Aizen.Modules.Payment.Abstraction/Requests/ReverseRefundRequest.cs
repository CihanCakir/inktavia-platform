namespace Aizen.Modules.Payment.Abstraction.Requests;

/// <summary>
/// Admin API request: reverses a processed refund record (e.g., issued in error).
/// The RefundRecord is marked Reversed — not deleted. TotalRefundedAmount is recalculated.
/// </summary>
public sealed class ReverseRefundRequest
{
    public required string  ReversalReason { get; init; }
    public          string? AdminNote      { get; init; }
}
