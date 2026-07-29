using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Dto;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Economics;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetProviderTransactions;

/// <summary>
/// Provider transactions read-model. BE-P8/S8: each row carries the immutable economics-snapshot breakdown (read from the
/// linked <c>PaymentEconomicsSnapshot</c>, null for legacy rows). BE-P10: refund/dispute status + the latest refund
/// allocation (clawback/advance transparency). All additive — existing fields unchanged.
/// </summary>
public sealed class GetProviderTransactionsQueryHandler
    : AizenQueryHandler<GetProviderTransactionsQuery, ProviderTransactionPagedResultDto>
{
    private readonly IPaymentTransactionRepository       _repo;
    private readonly IPaymentEconomicsSnapshotRepository _snapshots;
    private readonly IRefundAllocationRepository         _allocations;

    public GetProviderTransactionsQueryHandler(
        IPaymentTransactionRepository repo,
        IPaymentEconomicsSnapshotRepository snapshots,
        IRefundAllocationRepository allocations)
    {
        _repo        = repo;
        _snapshots   = snapshots;
        _allocations = allocations;
    }

    public override async Task<ProviderTransactionPagedResultDto?> Handle(
        GetProviderTransactionsQuery request, CancellationToken ct)
    {
        var skip   = (request.Page - 1) * request.PageSize;
        var status = request.Status.HasValue ? (PaymentTransactionStatus?)request.Status.Value : null;
        var type   = request.Type.HasValue   ? (TransactionType?)request.Type.Value           : null;

        var (items, total) = await _repo.GetProviderPagedAsync(
            request.ProviderProfileId, status, type, skip, request.PageSize, request.From, request.To, ct);

        var rows = new List<ProviderTransactionDto>(items.Count);
        foreach (var x in items)
        {
            // BE-P8/S8 — the immutable snapshot breakdown (never recomputed); null when the tx has no linked snapshot.
            ProviderTransactionEconomicsBreakdownDto? breakdown = null;
            if (x.EconomicsSnapshotId is { } snapId)
            {
                var s = await _snapshots.GetByIdAsync(snapId, ct);
                if (s is not null) breakdown = MapBreakdown(s);
            }

            // BE-P10 — the latest refund allocation on this transaction (only for refunded rows).
            ProviderTransactionRefundSummaryDto? refund = null;
            if (x.TotalRefundedAmount > 0m)
                refund = await LoadLatestRefundSummaryAsync(x.Id, ct);

            rows.Add(new ProviderTransactionDto
            {
                TransactionCode     = x.TransactionCode,
                TransactionType     = (int)x.TransactionType,
                ContextType         = (int)x.ContextType,
                ContextId           = x.ContextId,
                GrossAmount         = x.GrossAmount,
                CommissionAmount    = x.CommissionAmount,
                VatOnCommission     = x.VatOnCommission,
                NetPayoutAmount     = x.NetPayoutAmount,
                TotalRefundedAmount = x.TotalRefundedAmount,
                CurrencyCode        = x.CurrencyCode,
                Status              = (int)x.Status,
                GatewayReference    = x.GatewayReference,
                CreatedAt           = x.CreateDate ?? DateTime.MinValue,
                CapturedAt          = x.CapturedAt,
                ReleasedAt          = x.ReleasedAt,
                DisputedAt          = x.DisputedAt,
                EconomicsSnapshotId = x.EconomicsSnapshotId,
                EconomicsBreakdown  = breakdown,
                RefundSummary       = refund,
            });
        }

        return new ProviderTransactionPagedResultDto
        {
            Items = rows, Total = total, Page = request.Page, PageSize = request.PageSize,
        };
    }

    private async Task<ProviderTransactionRefundSummaryDto?> LoadLatestRefundSummaryAsync(long transactionId, CancellationToken ct)
    {
        var records = await _repo.GetRefundRecordsAsync(transactionId, ct);
        var latest = records
            .Where(r => r.RefundAllocationId.HasValue)
            .OrderByDescending(r => r.ProcessedAt ?? r.CreateDate ?? DateTime.MinValue)
            .ThenByDescending(r => r.Id)
            .FirstOrDefault();
        if (latest is null) return null;

        var a = await _allocations.GetByRefundRecordIdAsync(latest.Id, ct);
        if (a is null) return null;

        return new ProviderTransactionRefundSummaryDto
        {
            Cause                            = a.Cause.ToString(),
            ReleaseState                     = a.ReleaseState.ToString(),
            ServiceRefundAmount              = a.ServiceRefundAmount,
            ProviderNetReversalAmount        = a.ProviderNetReversalAmount,
            CommissionRevenueReversalAmount  = a.CommissionRevenueReversalAmount,
            PlatformAdvancedRefundAmount     = a.PlatformAdvancedRefundAmount,
            ProviderRecoveryAmount           = a.ProviderRecoveryAmount,
            RemainingProviderNegativeBalance = a.RemainingProviderNegativeBalance,
        };
    }

    private static ProviderTransactionEconomicsBreakdownDto MapBreakdown(PaymentEconomicsSnapshotEntity s) => new()
    {
        ServiceAmount               = s.ServiceAmountSnapshot,
        ServiceVatAmount            = s.ServiceVatAmountSnapshot,
        CommissionBaseAmount        = s.CommissionBaseAmountSnapshot,
        CommissionRate              = s.CommissionRateSnapshot,
        CommissionAmount            = s.CommissionAmountSnapshot,
        ProviderNetAmount           = s.ProviderNetAmountSnapshot,
        PlatformFeeNetAmount        = s.PlatformFeeNetAmountSnapshot,
        PlatformFeeVatAmount        = s.PlatformFeeVatAmountSnapshot,
        PlatformFeeGrossAmount      = s.PlatformFeeGrossAmountSnapshot,
        CustomerTotalAmount         = s.CustomerTotalAmountSnapshot,
        PlatformGrossShare          = s.PlatformGrossShareSnapshot,
        TotalCustomerDiscount       = s.TotalCustomerDiscountSnapshot,
        TotalProviderFundedDiscount = s.TotalProviderFundedDiscountSnapshot,
        TotalPlatformFundedDiscount = s.TotalPlatformFundedDiscountSnapshot,
    };
}
