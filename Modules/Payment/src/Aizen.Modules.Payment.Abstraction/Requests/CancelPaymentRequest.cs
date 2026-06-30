using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Abstraction.Requests;

/// <summary>
/// Admin API request: cancels a PendingIntent transaction before any money is captured.
/// No gateway call — intent is voided in-system.
/// For captured funds: use RefundPaymentRequest instead.
/// </summary>
public sealed class CancelPaymentRequest
{
    public required CancellationReason CancellationReason { get; init; }
    public          string?            AdminNote          { get; init; }
}
