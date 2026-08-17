using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Economics;
using Aizen.Modules.Payment.Domain.Money;

namespace Aizen.Modules.Payment.Domain.Entities.RefundAllocation;

/// <summary>
/// BE-P10 §7.5 — the immutable 9-amount refund breakdown, derived ENTIRELY from the immutable
/// <see cref="PaymentEconomicsSnapshotEntity"/> (never recompute rates). All money is 2-dp AwayFromZero via MoneyMath.
/// <para><b>Binding invariants (zero tolerance):</b> ServiceRefund == ProviderNetReversal + CommissionRevenueReversal;
/// PlatformFeeGross == PlatformFeeNet + PlatformFeeVat; TotalRefundToGateway == ServiceRefund + PlatformFeeGross. The
/// gateway refund <b>expense</b> is a separate cost, never deducted from the customer refund.</para>
/// </summary>
public sealed record RefundAllocation(
    decimal ServiceRefundAmount,
    decimal ProviderNetReversalAmount,
    decimal CommissionRevenueReversalAmount,
    decimal PlatformFeeNetRefundAmount,
    decimal PlatformFeeVatRefundAmount,
    decimal PlatformFeeGrossRefundAmount,
    decimal GatewayRefundExpenseAmount,
    decimal ProviderRecoveryAmount,
    decimal PlatformAdvancedRefundAmount,
    decimal RemainingProviderNegativeBalance)
{
    /// <summary>The customer-facing total sent to the gateway (§7.5). == ServiceRefund + PlatformFeeGross.</summary>
    public decimal TotalRefundToGateway => ServiceRefundAmount + PlatformFeeGrossRefundAmount;
}

/// <summary>
/// BE-P10 §2/§7.5 — the PURE, snapshot-driven refund allocation calculator. No I/O, no rate recomputation: every reversal
/// is a proportion of the immutable snapshot amounts so the parts sum exactly.
/// </summary>
public static class RefundAllocationCalculator
{
    public static RefundAllocation Resolve(
        PaymentEconomicsSnapshotEntity snapshot,
        decimal                        refundServiceAmount,
        RefundCause                    cause,
        ReleaseState                   releaseState,
        PlatformFeeRefundMode          platformFeeRefundMode,
        decimal?                       fixedPlatformFeeRefund = null,
        decimal                        gatewayRefundExpense   = 0m)
    {
        var serviceAmount = MoneyMath.Round(snapshot.ServiceAmountSnapshot);
        var serviceRefund = MoneyMath.Round(refundServiceAmount);
        if (serviceRefund < 0m || serviceAmount <= 0m || serviceRefund > serviceAmount)
            throw new AizenBusinessException((int)PaymentErrorCode.RefundAllocationMismatch,
                $"Refund service amount {serviceRefund} out of range for snapshot service {serviceAmount}.");

        // Proportional reversal from the snapshot (§7.5) — commission first, provider net = service − commission (exact sum).
        var ratio       = serviceRefund / serviceAmount;
        var commissionR = MoneyMath.Round(MoneyMath.Round(snapshot.CommissionAmountSnapshot) * ratio);
        var providerR   = serviceRefund - commissionR;   // guarantees ServiceRefund == providerR + commissionR

        // Platform fee refund per mode, on the snapshot fee net/vat/gross.
        var feeNet   = MoneyMath.Round(snapshot.PlatformFeeNetAmountSnapshot);
        var feeVat   = MoneyMath.Round(snapshot.PlatformFeeVatAmountSnapshot);
        var feeVatRate = feeNet > 0m ? feeVat / feeNet : 0m;

        decimal refNet, refVat;
        switch (platformFeeRefundMode)
        {
            case PlatformFeeRefundMode.Full:
                refNet = feeNet; refVat = feeVat; break;
            case PlatformFeeRefundMode.None:
                refNet = 0m; refVat = 0m; break;
            case PlatformFeeRefundMode.FixedAmount:
                refNet = Math.Min(MoneyMath.Round(fixedPlatformFeeRefund ?? 0m), feeNet);
                refVat = MoneyMath.Round(refNet * feeVatRate); break;
            case PlatformFeeRefundMode.ProRata:
            case PlatformFeeRefundMode.RuleBased:   // MVP: RuleBased == ProRata (documented)
            default:
                refNet = MoneyMath.Round(feeNet * ratio);
                refVat = MoneyMath.Round(feeVat * ratio); break;
        }
        var refGross = refNet + refVat;

        // §7.3: provider recovery only when already released; the un-recovered part (advanced / negative balance) is
        // finalised by the recovery step (handler) — 0 here.
        var providerRecovery = releaseState == ReleaseState.AfterProviderRelease ? providerR : 0m;

        var allocation = new RefundAllocation(
            ServiceRefundAmount:              serviceRefund,
            ProviderNetReversalAmount:        providerR,
            CommissionRevenueReversalAmount:  commissionR,
            PlatformFeeNetRefundAmount:       refNet,
            PlatformFeeVatRefundAmount:       refVat,
            PlatformFeeGrossRefundAmount:     refGross,
            GatewayRefundExpenseAmount:       MoneyMath.Round(gatewayRefundExpense),
            ProviderRecoveryAmount:           providerRecovery,
            PlatformAdvancedRefundAmount:     0m,
            RemainingProviderNegativeBalance: 0m);

        Verify(allocation);
        return allocation;
    }

    /// <summary>Re-checks the §7.5 zero-tolerance invariants; throws <see cref="PaymentErrorCode.RefundAllocationMismatch"/>.</summary>
    public static void Verify(RefundAllocation a)
    {
        if (a.ServiceRefundAmount != a.ProviderNetReversalAmount + a.CommissionRevenueReversalAmount)
            throw new AizenBusinessException((int)PaymentErrorCode.RefundAllocationMismatch,
                $"ServiceRefund {a.ServiceRefundAmount} != ProviderNetReversal {a.ProviderNetReversalAmount} + CommissionReversal {a.CommissionRevenueReversalAmount}.");
        if (a.PlatformFeeGrossRefundAmount != a.PlatformFeeNetRefundAmount + a.PlatformFeeVatRefundAmount)
            throw new AizenBusinessException((int)PaymentErrorCode.RefundAllocationMismatch,
                $"PlatformFeeGross {a.PlatformFeeGrossRefundAmount} != net {a.PlatformFeeNetRefundAmount} + vat {a.PlatformFeeVatRefundAmount}.");
        if (a.TotalRefundToGateway != a.ServiceRefundAmount + a.PlatformFeeGrossRefundAmount)
            throw new AizenBusinessException((int)PaymentErrorCode.RefundAllocationMismatch,
                $"TotalRefundToGateway {a.TotalRefundToGateway} != ServiceRefund + PlatformFeeGross.");
        if (a.ServiceRefundAmount < 0m || a.ProviderNetReversalAmount < 0m || a.CommissionRevenueReversalAmount < 0m
            || a.PlatformFeeGrossRefundAmount < 0m)
            throw new AizenBusinessException((int)PaymentErrorCode.RefundAllocationMismatch, "Negative refund component.");
    }
}
