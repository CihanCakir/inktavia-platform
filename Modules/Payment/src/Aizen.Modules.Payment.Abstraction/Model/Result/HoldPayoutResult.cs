namespace Aizen.Modules.Payment.Abstraction.Model.Result;

public sealed record HoldPayoutResult(
    long     PayoutRecordId,
    string   HoldReason,
    DateTime HeldAtUtc
);
