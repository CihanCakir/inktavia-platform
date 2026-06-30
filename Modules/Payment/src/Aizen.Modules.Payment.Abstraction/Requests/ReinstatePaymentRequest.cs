namespace Aizen.Modules.Payment.Abstraction.Requests;

/// <summary>
/// Admin API request: reinstates a cancelled transaction back to PendingIntent.
/// Use when a cancellation was made in error and the payer needs to retry.
/// Admin note is mandatory for audit trail.
/// </summary>
public sealed class ReinstatePaymentRequest
{
    public required string AdminNote { get; init; }
}
