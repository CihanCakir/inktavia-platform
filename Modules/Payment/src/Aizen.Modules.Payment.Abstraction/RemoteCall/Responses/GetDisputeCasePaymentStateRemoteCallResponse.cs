namespace Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;

/// <summary>
/// BE-S13a — the cost-free P10 payment/refund state + S8 economics for a dispute case file. CONFIDENTIALITY (§20.9):
/// carries NO supplier cost / dealer margin — only the derived, cost-free economics that already leave Payment.
/// </summary>
public sealed class GetDisputeCasePaymentStateRemoteCallResponse
{
    public bool HasTransaction { get; init; }
    public long? TransactionId { get; init; }
    public string? TransactionCode { get; init; }
    public string? Status { get; init; }
    public string CurrencyCode { get; init; } = "TRY";
    public decimal GrossAmount { get; init; }
    public decimal TotalRefundedAmount { get; init; }
    /// <summary>GrossAmount − TotalRefundedAmount — the ceiling for a dispute partial refund.</summary>
    public decimal RefundableAmount { get; init; }
    public bool EscrowReleased { get; init; }
    public DateTime? CapturedAt { get; init; }
    public DateTime? ReleasedAt { get; init; }

    public DisputeEconomicsRemoteDto? Economics { get; init; }
    public List<DisputeRefundAllocationRemoteDto> RefundAllocations { get; init; } = new();
    public DisputeChargebackRemoteDto? Chargeback { get; init; }
}

public sealed class DisputeEconomicsRemoteDto
{
    public long SnapshotId { get; init; }
    public string SnapshotCode { get; init; } = default!;
    public string CurrencyCode { get; init; } = "TRY";
    public decimal ServiceAmount { get; init; }
    public decimal CommissionAmount { get; init; }
    public decimal ProviderNetAmount { get; init; }
    public decimal PlatformFeeGrossAmount { get; init; }
    public decimal CustomerTotalAmount { get; init; }
    public List<DisputeEconomicsLineRemoteDto> Lines { get; init; } = new();
}

public sealed class DisputeEconomicsLineRemoteDto
{
    public string LineRef { get; init; } = default!;
    public string? ItemType { get; init; }
    public decimal GrossBeforeDiscount { get; init; }
    public decimal CustomerDiscountAmount { get; init; }
    public decimal CommissionAmount { get; init; }
    public decimal ProviderNetAmount { get; init; }
    public decimal LineVatAmount { get; init; }
    public decimal LineTotalAmount { get; init; }
}

public sealed class DisputeRefundAllocationRemoteDto
{
    public long RefundRecordId { get; init; }
    public string RefundCode { get; init; } = default!;
    public string RefundReason { get; init; } = default!;
    public string? RefundCause { get; init; }
    public decimal Amount { get; init; }
    public string RefundStatus { get; init; } = default!;
    public decimal ServiceRefundAmount { get; init; }
    public decimal ProviderNetReversalAmount { get; init; }
    public decimal CommissionRevenueReversalAmount { get; init; }
    public decimal PlatformFeeGrossRefundAmount { get; init; }
    public decimal PlatformAdvancedRefundAmount { get; init; }
    public DateTime? ProcessedAt { get; init; }
}

public sealed class DisputeChargebackRemoteDto
{
    public string GatewayChargebackReference { get; init; } = default!;
    public decimal Amount { get; init; }
    public decimal ProviderRecoveredAmount { get; init; }
    public decimal RemainingNegativeBalance { get; init; }
    public DateTime ReceivedAtUtc { get; init; }
}
