using Aizen.Core.Domain;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.RefundAllocation;

/// <summary>
/// BE-P10 §7.5 — the persisted 9-amount refund breakdown (immutable), linked to the refund record + the economics snapshot
/// it was derived from. Written once per refund; the amounts come straight from <see cref="RefundAllocationCalculator"/>.
/// </summary>
[DocumentationInfo("Refund allocation entity", "Immutable §7.5 nine-amount refund breakdown derived from the economics snapshot.")]
[NoMessagebusSync] // domain-authored immutable P10 refund breakdown — never generically writable
public sealed class RefundAllocationEntity : AizenEntityWithAudit
{
    public long         RefundRecordId       { get; private set; }
    public long         EconomicsSnapshotId  { get; private set; }
    public RefundCause  Cause                { get; private set; }
    public ReleaseState ReleaseState         { get; private set; }
    public string       CurrencyCode         { get; private set; } = "TRY";

    public decimal ServiceRefundAmount              { get; private set; }
    public decimal ProviderNetReversalAmount        { get; private set; }
    public decimal CommissionRevenueReversalAmount  { get; private set; }
    public decimal PlatformFeeNetRefundAmount       { get; private set; }
    public decimal PlatformFeeVatRefundAmount       { get; private set; }
    public decimal PlatformFeeGrossRefundAmount     { get; private set; }
    public decimal GatewayRefundExpenseAmount       { get; private set; }
    public decimal ProviderRecoveryAmount           { get; private set; }
    public decimal PlatformAdvancedRefundAmount     { get; private set; }
    public decimal RemainingProviderNegativeBalance { get; private set; }

    private RefundAllocationEntity() { }

    public static RefundAllocationEntity Create(
        long refundRecordId, long economicsSnapshotId, RefundCause cause, ReleaseState releaseState,
        string currencyCode, RefundAllocation a)
        => new()
        {
            RefundRecordId                   = refundRecordId,
            EconomicsSnapshotId              = economicsSnapshotId,
            Cause                            = cause,
            ReleaseState                     = releaseState,
            CurrencyCode                     = currencyCode.ToUpperInvariant(),
            ServiceRefundAmount              = a.ServiceRefundAmount,
            ProviderNetReversalAmount        = a.ProviderNetReversalAmount,
            CommissionRevenueReversalAmount  = a.CommissionRevenueReversalAmount,
            PlatformFeeNetRefundAmount       = a.PlatformFeeNetRefundAmount,
            PlatformFeeVatRefundAmount       = a.PlatformFeeVatRefundAmount,
            PlatformFeeGrossRefundAmount     = a.PlatformFeeGrossRefundAmount,
            GatewayRefundExpenseAmount       = a.GatewayRefundExpenseAmount,
            ProviderRecoveryAmount           = a.ProviderRecoveryAmount,
            PlatformAdvancedRefundAmount     = a.PlatformAdvancedRefundAmount,
            RemainingProviderNegativeBalance = a.RemainingProviderNegativeBalance,
            IsActive                         = true,
        };
}

/// <summary>
/// BE-P10 §21.2 — a chargeback (up to 13 months post-transaction). Uses the §7.3 release-after recovery path against the
/// provider + a distinct <see cref="ChargebackExpenseAmount"/> (reporting = P12). Idempotent on the gateway reference.
/// </summary>
[DocumentationInfo("Chargeback record entity", "A gateway chargeback → release-after clawback + ChargebackExpense. Idempotent on the gateway reference.")]
[NoMessagebusSync] // domain-authored immutable P10 chargeback record — never generically writable
public sealed class ChargebackRecordEntity : AizenEntityWithAudit
{
    public long     PaymentTransactionId      { get; private set; }
    public string   GatewayChargebackReference { get; private set; } = default!;
    public decimal  Amount                    { get; private set; }
    public string   CurrencyCode              { get; private set; } = "TRY";
    public decimal  ChargebackExpenseAmount   { get; private set; }
    public decimal  ProviderRecoveredAmount   { get; private set; }
    public decimal  RemainingNegativeBalance  { get; private set; }
    public DateTime ReceivedAtUtc             { get; private set; }
    public string?  Notes                     { get; private set; }

    private ChargebackRecordEntity() { }

    public static ChargebackRecordEntity Create(
        long paymentTransactionId, string gatewayChargebackReference, decimal amount, string currencyCode,
        decimal chargebackExpenseAmount, DateTime receivedAtUtc, string? notes = null)
        => new()
        {
            PaymentTransactionId       = paymentTransactionId,
            GatewayChargebackReference = gatewayChargebackReference,
            Amount                     = amount,
            CurrencyCode               = currencyCode.ToUpperInvariant(),
            ChargebackExpenseAmount    = chargebackExpenseAmount,
            ReceivedAtUtc              = receivedAtUtc,
            Notes                      = notes,
            IsActive                   = true,
        };

    public void SetRecovery(decimal recovered, decimal remainingNegative)
    {
        ProviderRecoveredAmount  = recovered;
        RemainingNegativeBalance = remainingNegative;
    }
}
