namespace Aizen.Modules.Payment.Abstraction.Model.Result;

public sealed record MarkPayoutCompleteResult(
    long     PayoutRecordId,
    string   GatewayPayoutId,
    DateTime CompletedAtUtc
);
