namespace Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;

/// <summary>
/// BE-S13b — result of driving the P10 path for a dispute resolution. When the SR has no transaction (no escrow was
/// ever held) <see cref="Applied"/> is false and no money moved. On an idempotent re-resolve the prior result is
/// echoed (<see cref="AlreadyApplied"/> true) and nothing is refunded/released again.
/// </summary>
public sealed class ResolveDisputeOutcomeRemoteCallResponse
{
    /// <summary>True when a refund or an escrow release actually ran (or had already run — see <see cref="AlreadyApplied"/>).</summary>
    public bool Applied { get; init; }

    /// <summary>True when a prior resolve already applied this dispute's outcome (idempotent no-op this time).</summary>
    public bool AlreadyApplied { get; init; }

    /// <summary>True when the outcome released escrow to the provider (no refund).</summary>
    public bool EscrowReleased { get; init; }

    /// <summary>Amount refunded to the payer (0 for release / no-transaction).</summary>
    public decimal RefundedAmount { get; init; }

    public long? RefundRecordId { get; init; }
    public long? PayoutRecordId { get; init; }
    public string? Message { get; init; }
}
