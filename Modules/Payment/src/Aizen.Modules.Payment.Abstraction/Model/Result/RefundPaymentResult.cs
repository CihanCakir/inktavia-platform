namespace Aizen.Modules.Payment.Abstraction.Model.Result;

public sealed record RefundPaymentResult(
    long    RefundRecordId,
    string  RefundCode,
    string  GatewayRefundReference,
    decimal RefundedAmount
);
