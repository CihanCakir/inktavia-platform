namespace Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;

/// <summary>
/// BE-S11b — SR → Payment: apply a change-order <b>reduction</b> (removed/reduced work) by refunding the delta against the
/// SR's original escrow via the P10 rails (reuses the gateway + <c>RefundAllocationService</c> — no bespoke refund math and
/// no mutation of the accepted snapshot). Idempotent on the change-order context ref <c>SR-{sr}-OFFER-{offer}-CO-{id}</c>
/// (stamped on the refund record's AdminNote) so a re-apply never double-refunds.
/// </summary>
public sealed class ApplyChangeOrderReductionRemoteCallRequest
{
    public required long ServiceRequestId { get; init; }
    public required long AcceptedOfferId  { get; init; }
    public required long ChangeOrderId    { get; init; }

    /// <summary>The reduction amount to refund to the customer (customer-facing, strictly positive). Validated ≤ refundable in Payment.</summary>
    public required decimal ReductionAmount { get; init; }

    /// <summary>The Payment <c>RefundReason</c> int code — mapped to a RefundCause via RefundCauseMap.</summary>
    public int RefundReasonCode { get; init; }

    public string? Notes { get; init; }
}
