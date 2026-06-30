namespace Aizen.Modules.Payment.Abstraction.Model.Result;

public sealed record ReinstateCancelledTransactionResult(
    long     TransactionId,
    string   TransactionCode,
    DateTime ReinstatedAtUtc
);
