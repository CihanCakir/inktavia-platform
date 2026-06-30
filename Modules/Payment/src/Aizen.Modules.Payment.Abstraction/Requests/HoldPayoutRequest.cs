namespace Aizen.Modules.Payment.Abstraction.Requests;

/// <summary>
/// Admin API request: places a pending payout on administrative hold.
/// </summary>
public sealed class HoldPayoutRequest
{
    public required string Reason    { get; init; }
    public          string? AdminNote { get; init; }
}
