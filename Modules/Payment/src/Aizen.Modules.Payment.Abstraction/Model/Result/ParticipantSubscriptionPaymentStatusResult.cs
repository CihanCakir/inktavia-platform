namespace Aizen.Modules.Payment.Abstraction.Model.Result;

/// <summary>
/// BE-MO7 — the owner-facing payment status of a participant subscription's checkout transaction (paid plan). Mirrors
/// the MO3 owner payment lifecycle so the mobile FE can reuse the same poll: <c>None</c> (no transaction),
/// <c>Pending</c> (awaiting capture), <c>Paid</c> (captured → the subscription is now active), <c>Failed</c>,
/// <c>Cancelled</c>. Cost-free: the customer amount + status + timestamp only, never commission / net-payout.
/// </summary>
public sealed record ParticipantSubscriptionPaymentStatusResult(
    bool      HasPayment,
    long?     TransactionId,
    string    Status,            // None | Pending | Paid | Failed | Cancelled
    string?   RawStatus,
    decimal?  Amount,
    string?   CurrencyCode,
    System.DateTime? PaidAt);
