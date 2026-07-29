namespace Aizen.Modules.Payment.Abstraction.Model.Result;

/// <summary>BE-P10 §21.2 — the outcome of recording a chargeback (release-after provider recovery + expense).</summary>
public sealed record RecordChargebackResult(
    long    ChargebackRecordId,
    long    TransactionId,
    string  GatewayChargebackReference,
    decimal ChargebackAmount,
    decimal ProviderRecoveredAmount,
    decimal RemainingNegativeBalance,
    decimal ChargebackExpenseAmount,
    bool    AlreadyProcessed
);
