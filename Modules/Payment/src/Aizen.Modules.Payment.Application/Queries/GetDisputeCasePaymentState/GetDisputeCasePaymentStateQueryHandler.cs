using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;
using Aizen.Modules.Payment.Domain.Entities.Transaction;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetDisputeCasePaymentState;

/// <summary>
/// BE-S13a — composes the cost-free P10 payment/refund state + S8 economics for a dispute case file. Pure read, no
/// state written. CONFIDENTIALITY (§20.9): only the derived, cost-free economics fields are projected — supplier list
/// price / dealer margin live in <c>PartCommercialTermEntity</c> and are never touched here.
/// </summary>
public sealed class GetDisputeCasePaymentStateQueryHandler
    : AizenQueryHandler<GetDisputeCasePaymentStateQuery, GetDisputeCasePaymentStateRemoteCallResponse>
{
    private readonly IPaymentTransactionRepository       _transactions;
    private readonly IPaymentEconomicsSnapshotRepository _snapshots;
    private readonly IRefundAllocationRepository         _allocations;
    private readonly IChargebackRecordRepository         _chargebacks;

    public GetDisputeCasePaymentStateQueryHandler(
        IPaymentTransactionRepository       transactions,
        IPaymentEconomicsSnapshotRepository snapshots,
        IRefundAllocationRepository         allocations,
        IChargebackRecordRepository         chargebacks)
    {
        _transactions = transactions;
        _snapshots    = snapshots;
        _allocations  = allocations;
        _chargebacks  = chargebacks;
    }

    public override async Task<GetDisputeCasePaymentStateRemoteCallResponse?> Handle(
        GetDisputeCasePaymentStateQuery request, CancellationToken ct)
    {
        var tx = await _transactions.GetByContextAsync(
            TransactionContextType.ServiceRequest, request.Request.ServiceRequestId, ct);

        if (tx is null)
            return new GetDisputeCasePaymentStateRemoteCallResponse { HasTransaction = false };

        var economics  = await BuildEconomicsAsync(tx, ct);
        var allocations = await BuildRefundAllocationsAsync(tx, ct);
        var chargeback = await BuildChargebackAsync(tx, ct);

        return new GetDisputeCasePaymentStateRemoteCallResponse
        {
            HasTransaction      = true,
            TransactionId       = tx.Id,
            TransactionCode     = tx.TransactionCode,
            Status              = tx.Status.ToString(),
            CurrencyCode        = tx.CurrencyCode,
            GrossAmount         = tx.GrossAmount,
            TotalRefundedAmount = tx.TotalRefundedAmount,
            RefundableAmount    = tx.GrossAmount - tx.TotalRefundedAmount,
            EscrowReleased      = tx.ReleasedAt.HasValue,
            CapturedAt          = tx.CapturedAt,
            ReleasedAt          = tx.ReleasedAt,
            Economics           = economics,
            RefundAllocations   = allocations,
            Chargeback          = chargeback,
        };
    }

    private async Task<DisputeEconomicsRemoteDto?> BuildEconomicsAsync(
        PaymentTransactionEntity tx, CancellationToken ct)
    {
        if (tx.EconomicsSnapshotId is not { } snapId) return null;
        var snap = await _snapshots.GetByIdWithLinesAsync(snapId, ct);
        if (snap is null) return null;

        return new DisputeEconomicsRemoteDto
        {
            SnapshotId             = snap.Id,
            SnapshotCode           = snap.SnapshotCode,
            CurrencyCode           = snap.CurrencyCodeSnapshot,
            ServiceAmount          = snap.ServiceAmountSnapshot,
            CommissionAmount       = snap.CommissionAmountSnapshot,
            ProviderNetAmount      = snap.ProviderNetAmountSnapshot,
            PlatformFeeGrossAmount = snap.PlatformFeeGrossAmountSnapshot,
            CustomerTotalAmount    = snap.CustomerTotalAmountSnapshot,
            Lines = snap.OfferLines
                .OrderBy(l => l.SortOrder)
                .Select(l => new DisputeEconomicsLineRemoteDto
                {
                    LineRef                = l.LineRef,
                    ItemType               = l.ItemType.ToString(),
                    GrossBeforeDiscount    = l.LineGrossBeforeDiscount,
                    CustomerDiscountAmount = l.CustomerDiscountAmount,
                    CommissionAmount       = l.CommissionAmount,
                    ProviderNetAmount      = l.ProviderNetAmount,
                    LineVatAmount          = l.LineVatAmount,
                    LineTotalAmount        = l.LineTotalAmount,
                })
                .ToList(),
        };
    }

    private async Task<List<DisputeRefundAllocationRemoteDto>> BuildRefundAllocationsAsync(
        PaymentTransactionEntity tx, CancellationToken ct)
    {
        var records = await _transactions.GetRefundRecordsAsync(tx.Id, ct);
        var result  = new List<DisputeRefundAllocationRemoteDto>(records.Count);

        foreach (var record in records)
        {
            var alloc = await _allocations.GetByRefundRecordIdAsync(record.Id, ct);
            result.Add(new DisputeRefundAllocationRemoteDto
            {
                RefundRecordId                  = record.Id,
                RefundCode                      = record.RefundCode,
                RefundReason                    = record.Reason.ToString(),
                RefundCause                     = record.Cause?.ToString(),
                Amount                          = record.Amount,
                RefundStatus                    = record.Status.ToString(),
                ServiceRefundAmount             = alloc?.ServiceRefundAmount ?? 0m,
                ProviderNetReversalAmount       = alloc?.ProviderNetReversalAmount ?? 0m,
                CommissionRevenueReversalAmount = alloc?.CommissionRevenueReversalAmount ?? 0m,
                PlatformFeeGrossRefundAmount    = alloc?.PlatformFeeGrossRefundAmount ?? 0m,
                PlatformAdvancedRefundAmount    = alloc?.PlatformAdvancedRefundAmount ?? 0m,
                ProcessedAt                     = record.ProcessedAt,
            });
        }

        return result;
    }

    private async Task<DisputeChargebackRemoteDto?> BuildChargebackAsync(
        PaymentTransactionEntity tx, CancellationToken ct)
    {
        var cb = await _chargebacks.GetByTransactionIdAsync(tx.Id, ct);
        if (cb is null) return null;

        return new DisputeChargebackRemoteDto
        {
            GatewayChargebackReference = cb.GatewayChargebackReference,
            Amount                     = cb.Amount,
            ProviderRecoveredAmount    = cb.ProviderRecoveredAmount,
            RemainingNegativeBalance   = cb.RemainingNegativeBalance,
            ReceivedAtUtc              = cb.ReceivedAtUtc,
        };
    }
}
