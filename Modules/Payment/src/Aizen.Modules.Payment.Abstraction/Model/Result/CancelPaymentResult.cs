namespace Aizen.Modules.Payment.Abstraction.Model.Result;

public sealed record CancelPaymentResult(
    long     TransactionId,
    string   TransactionCode,
    DateTime CancelledAtUtc,
    bool     WasAlreadyCancelled
);
