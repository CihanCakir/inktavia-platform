namespace Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;

/// <summary>
/// BE-S11b — result of applying a change-order reduction through the P10 rails. When the SR has no transaction (no escrow
/// was ever held) <see cref="Applied"/> is false and no money moved. On an idempotent re-apply the prior refund is echoed
/// (<see cref="AlreadyApplied"/> true) and nothing is refunded again.
/// </summary>
public sealed class ApplyChangeOrderReductionRemoteCallResponse
{
    public bool Applied { get; init; }
    public bool AlreadyApplied { get; init; }
    public decimal RefundedAmount { get; init; }
    public long? RefundRecordId { get; init; }
    /// <summary>The original SR transaction the refund hit (echoed for the change-order link).</summary>
    public long? TransactionId { get; init; }
    public string? Message { get; init; }
}
