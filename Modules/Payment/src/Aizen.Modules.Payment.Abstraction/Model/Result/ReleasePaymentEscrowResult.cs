namespace Aizen.Modules.Payment.Abstraction.Model.Result;

public sealed record ReleasePaymentEscrowResult(
    long    PayoutRecordId,
    string  GatewayPayoutId,
    decimal ProviderNetAmount
);
