using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Abstraction.Requests;

/// <summary>
/// Admin API request: issues a full or partial gateway refund.
/// Applicable to Captured / Released / PartiallyRefunded transactions.
/// Creates a TransactionRefundRecord and updates TotalRefundedAmount.
/// </summary>
public sealed class RefundPaymentRequest
{
    public required decimal      RefundAmount { get; init; }
    public required RefundReason Reason       { get; init; }
    public required RefundType   RefundType   { get; init; }
    public          string?      AdminNote    { get; init; }
}
