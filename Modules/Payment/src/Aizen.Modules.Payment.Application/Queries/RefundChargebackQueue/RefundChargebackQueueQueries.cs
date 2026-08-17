using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Payment.Application.Queries.RefundChargebackQueue;

// ─── Refund queue (paged over TransactionRefundRecord; filter by cause/state/status) ──
public sealed class GetRefundQueueQuery : AizenQuery<RefundQueuePagedDto>
{
    public RefundCause?             Cause        { get; init; }
    public ReleaseState?            ReleaseState { get; init; }
    public TransactionRefundStatus? Status       { get; init; }
    public int Page     { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed class GetRefundQueueQueryHandler : AizenQueryHandler<GetRefundQueueQuery, RefundQueuePagedDto>
{
    private readonly PaymentDbContext _db;
    public GetRefundQueueQueryHandler(PaymentDbContext db) => _db = db;

    public override async Task<RefundQueuePagedDto?> Handle(GetRefundQueueQuery request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var size = request.PageSize is < 1 or > 200 ? 20 : request.PageSize;

        var q = _db.TransactionRefunds.AsNoTracking().AsQueryable();
        if (request.Cause.HasValue)        q = q.Where(x => x.Cause == request.Cause.Value);
        if (request.ReleaseState.HasValue) q = q.Where(x => x.ReleaseState == request.ReleaseState.Value);
        if (request.Status.HasValue)       q = q.Where(x => x.Status == request.Status.Value);

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(x => x.Id).Skip((page - 1) * size).Take(size)
            .Select(x => new RefundQueueItemDto
            {
                Id = x.Id, PaymentTransactionId = x.PaymentTransactionId, RefundCode = x.RefundCode,
                Amount = x.Amount, CurrencyCode = x.CurrencyCode, RefundType = (int)x.RefundType, Reason = (int)x.Reason,
                Status = (int)x.Status, Cause = x.Cause.HasValue ? (int)x.Cause.Value : (int?)null,
                ReleaseState = x.ReleaseState.HasValue ? (int)x.ReleaseState.Value : (int?)null,
                RefundAllocationId = x.RefundAllocationId, GatewayRefundReference = x.GatewayRefundReference,
                ProcessedAt = x.ProcessedAt, CreatedAt = x.CreateDate,
            }).ToListAsync(ct);

        return new RefundQueuePagedDto { Items = items, Total = total, Page = page, PageSize = size };
    }
}

// ─── Chargeback queue (paged over ChargebackRecord) ──────────────────────────
public sealed class GetChargebackQueueQuery : AizenQuery<ChargebackQueuePagedDto>
{
    public int Page     { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed class GetChargebackQueueQueryHandler : AizenQueryHandler<GetChargebackQueueQuery, ChargebackQueuePagedDto>
{
    private readonly PaymentDbContext _db;
    public GetChargebackQueueQueryHandler(PaymentDbContext db) => _db = db;

    public override async Task<ChargebackQueuePagedDto?> Handle(GetChargebackQueueQuery request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var size = request.PageSize is < 1 or > 200 ? 20 : request.PageSize;

        var q = _db.ChargebackRecords.AsNoTracking();
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(x => x.Id).Skip((page - 1) * size).Take(size)
            .Select(x => new ChargebackQueueItemDto
            {
                Id = x.Id, PaymentTransactionId = x.PaymentTransactionId, GatewayChargebackReference = x.GatewayChargebackReference,
                Amount = x.Amount, CurrencyCode = x.CurrencyCode, ChargebackExpenseAmount = x.ChargebackExpenseAmount,
                ProviderRecoveredAmount = x.ProviderRecoveredAmount, RemainingNegativeBalance = x.RemainingNegativeBalance,
                ReceivedAtUtc = x.ReceivedAtUtc, Notes = x.Notes,
            }).ToListAsync(ct);

        return new ChargebackQueuePagedDto { Items = items, Total = total, Page = page, PageSize = size };
    }
}
