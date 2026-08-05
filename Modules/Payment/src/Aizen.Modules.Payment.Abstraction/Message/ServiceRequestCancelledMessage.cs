using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.Payment.Abstraction.Message;

/// <summary>
/// Inbound message from the ServiceRequest module when a service request is cancelled.
/// Payment module listens to trigger a refund if the payment has been captured but not yet released.
///
/// Published by: Aizen.Modules.ServiceRequest when SR status → Cancelled
/// Exchange/queue: payment.service-request.cancelled
///
/// Behaviour:
///   - If transaction is PendingIntent or Cancelled → no-op
///   - If transaction is Captured (escrow) → full gateway refund
///   - If transaction is Released → no-op (funds already transferred, manual dispute required)
/// </summary>
public sealed class ServiceRequestCancelledMessage : AizenBaseMessage
{
    public long ServiceRequestId { get; init; }
    public long OfferId          { get; init; }
    public long PayerProfileId   { get; init; }

    /// <summary>Reason passed to the refund flow.</summary>
    public string CancellationReason { get; init; } = "ServiceRequestCancelled";

    /// <summary>
    /// N-E — mapped Payment <c>RefundReason</c> (int value) derived from the SR structured cancel reason.
    /// Drives <c>RefundCauseMap.FromReason</c> → allocation deterministically. 0 / unset ⇒ fall back to
    /// <c>RefundReason.ServiceRequestCancelled</c>.
    /// </summary>
    public int RefundReasonCode { get; init; }

    public DateTime CancelledAtUtc { get; init; }
}
