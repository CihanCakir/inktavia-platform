namespace Aizen.Modules.Payment.Abstraction.Requests;

/// <summary>
/// Admin API request: manually approves and releases a held payout.
/// </summary>
public sealed class ApproveManualPayoutRequest
{
    public required string  GatewayPayoutId { get; init; }
    public          string? AdminNote        { get; init; }
}
