namespace Aizen.Modules.Payment.Abstraction.Model.Result;

public sealed record CancelSubscriptionResult(
    long     SubscriptionId,
    string   Status,
    DateTime CancelledAt
);
