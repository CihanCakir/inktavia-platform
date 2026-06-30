namespace Aizen.Modules.Payment.Abstraction.Requests;

/// <summary>
/// Admin API request: marks a pending payout as completed.
/// In manual gateway mode: admin has physically transferred funds and confirms here.
/// In Iyzico mode: used for exception handling (failed auto-payouts).
/// </summary>
public sealed class MarkPayoutCompleteRequest
{
    public required string  GatewayPayoutId { get; init; }
    public          string? AdminNote       { get; init; }
}
