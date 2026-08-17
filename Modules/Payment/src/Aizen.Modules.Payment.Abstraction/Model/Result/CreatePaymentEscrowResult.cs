namespace Aizen.Modules.Payment.Abstraction.Model.Result;

public sealed record CreatePaymentEscrowResult(
    long    TransactionId,
    string  TransactionCode,
    string  GatewayReference,
    decimal CommissionRate,
    decimal CommissionAmount,
    decimal NetPayoutAmount
);
