namespace Aizen.Modules.Payment.Abstraction.Model.Result;

public sealed record ReversePartialRefundResult(
    long     RefundRecordId,
    string   RefundCode,
    decimal  ReversedAmount,
    DateTime ReversedAtUtc
);
