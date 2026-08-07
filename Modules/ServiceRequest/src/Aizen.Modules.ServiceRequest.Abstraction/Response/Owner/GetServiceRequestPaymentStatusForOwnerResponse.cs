namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Owner;

/// <summary>
/// BE-MO3 — the owner-facing payment status of an accepted service request's escrow transaction. Cost-free: the
/// customer total (what the owner paid) + lifecycle status + timestamps only; never provider cost / commission /
/// net-payout. When the SR has no payment transaction yet (offer not accepted) <see cref="HasPayment"/> is false
/// and <see cref="Status"/> is <c>None</c>. Drives the owner "did my payment go through?" poll after accept.
/// </summary>
[DocumentationInfo("Owner payment-status response", "Payment lifecycle state of the owner's accepted service request (cost-free).")]
public sealed class GetServiceRequestPaymentStatusForOwnerResponse
{
    public long ServiceRequestId { get; init; }

    /// <summary>False until the owner accepts an offer (which creates the escrow transaction).</summary>
    public bool HasPayment { get; init; }

    public long? TransactionId { get; init; }

    /// <summary>
    /// Owner-facing lifecycle: <c>None</c> (not accepted yet), <c>Pending</c> (awaiting capture), <c>Paid</c>
    /// (captured/released), <c>Failed</c>, or <c>Cancelled</c>. Mapped from the payment transaction status so the
    /// FE stays decoupled from the Payment enum.
    /// </summary>
    public string Status { get; init; } = "None";

    /// <summary>The raw <c>PaymentTransactionStatus</c> name (for telemetry/diagnostics), null when no payment.</summary>
    public string? RawStatus { get; init; }

    /// <summary>Customer total — what the owner pays. Cost-free (customer-facing amount, not provider economics).</summary>
    public decimal? Amount { get; init; }

    public string? CurrencyCode { get; init; }

    public DateTime? PaidAt { get; init; }
}
