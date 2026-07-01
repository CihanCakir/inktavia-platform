namespace Aizen.Modules.Payment.Abstraction.Model.Result;

public sealed record ApproveManualPayoutResult(
    long     PayoutRecordId,
    string   GatewayPayoutId,
    DateTime CompletedAtUtc
);
