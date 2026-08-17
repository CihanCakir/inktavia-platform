using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Dto;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.RefundAllocation;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Payment.Application.ProviderBalanceAdmin;

internal static class ProviderBalanceMapper
{
    public static ProviderBalanceAdminDto Map(ProviderBalanceEntity b, bool withMovements) => new()
    {
        Id = b.Id, ProviderProfileId = b.ProviderProfileId, CurrencyCode = b.CurrencyCode,
        Balance = b.Balance, NegativeAmount = b.NegativeAmount, NegativeBalanceLimit = b.NegativeBalanceLimit,
        IsOverLimit = b.IsOverLimit(),
        Movements = withMovements
            ? b.Movements.OrderByDescending(m => m.OccurredAtUtc).Select(m => new ProviderBalanceMovementDto
            {
                Id = m.Id, MovementType = (int)m.MovementType, Amount = m.Amount, BalanceAfter = m.BalanceAfter,
                RefundRecordId = m.RefundRecordId, ChargebackRecordId = m.ChargebackRecordId, AdminUserId = m.AdminUserId,
                Note = m.Note, OccurredAtUtc = m.OccurredAtUtc,
            }).ToList()
            : new(),
    };
}

// ─── List (paged) ────────────────────────────────────────────────────────────
public sealed class GetProviderBalancesQuery : AizenQuery<ProviderBalancePagedDto>
{
    public string? Currency     { get; init; }
    public bool    OnlyNegative { get; init; }
    public int Page     { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed class GetProviderBalancesQueryHandler : AizenQueryHandler<GetProviderBalancesQuery, ProviderBalancePagedDto>
{
    private readonly PaymentDbContext _db;
    public GetProviderBalancesQueryHandler(PaymentDbContext db) => _db = db;

    public override async Task<ProviderBalancePagedDto?> Handle(GetProviderBalancesQuery request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var size = request.PageSize is < 1 or > 200 ? 20 : request.PageSize;

        var q = _db.ProviderBalances.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Currency)) { var c = request.Currency.ToUpperInvariant(); q = q.Where(x => x.CurrencyCode == c); }
        if (request.OnlyNegative) q = q.Where(x => x.Balance < 0m);

        var total = await q.CountAsync(ct);
        var rows  = await q.OrderBy(x => x.Balance).Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return new ProviderBalancePagedDto
        {
            Items = rows.Select(b => ProviderBalanceMapper.Map(b, withMovements: false)).ToList(),
            Total = total, Page = page, PageSize = size,
        };
    }
}

// ─── By provider (with movements) ────────────────────────────────────────────
public sealed class GetProviderBalanceByProviderQuery : AizenQuery<ProviderBalanceAdminDto>
{
    public long   ProviderProfileId { get; init; }
    public string Currency          { get; init; } = "TRY";
}

public sealed class GetProviderBalanceByProviderQueryHandler : AizenQueryHandler<GetProviderBalanceByProviderQuery, ProviderBalanceAdminDto>
{
    private readonly IProviderBalanceRepository _balances;
    public GetProviderBalanceByProviderQueryHandler(IProviderBalanceRepository balances) => _balances = balances;

    public override async Task<ProviderBalanceAdminDto?> Handle(GetProviderBalanceByProviderQuery request, CancellationToken ct)
    {
        var b = await _balances.GetByProviderAsync(request.ProviderProfileId, request.Currency, ct);
        return b is null ? null : ProviderBalanceMapper.Map(b, withMovements: true);
    }
}

// ─── Manual adjust (audited) ─────────────────────────────────────────────────
public sealed class AdjustProviderBalanceCommand : AizenCommand<ProviderBalanceAdjustResultDto>
{
    public long    ProviderProfileId { get; init; }
    public string  CurrencyCode      { get; init; } = "TRY";
    public decimal SignedAmount      { get; init; }   // + credit (reduces owed), − debit
    public long    AdminUserId       { get; init; }
    public string  Note              { get; init; } = default!;
}

public sealed class AdjustProviderBalanceCommandHandler : AizenCommandHandler<AdjustProviderBalanceCommand, ProviderBalanceAdjustResultDto>
{
    private readonly IProviderBalanceRepository _balances;
    public AdjustProviderBalanceCommandHandler(IProviderBalanceRepository balances) => _balances = balances;

    public override async Task<ProviderBalanceAdjustResultDto?> Handle(AdjustProviderBalanceCommand r, CancellationToken ct)
    {
        if (r.SignedAmount == 0m)
            throw new AizenBusinessException((int)PaymentErrorCode.RefundAllocationPolicyInvalid, "Adjustment amount must be non-zero.");

        var balance = await _balances.GetByProviderAsync(r.ProviderProfileId, r.CurrencyCode, ct);
        var isNew = balance is null;
        if (balance is null)
        {
            balance = ProviderBalanceEntity.Create(r.ProviderProfileId, r.CurrencyCode, 0m);
            await _balances.AddAsync(balance, ct);
        }

        balance.ManualAdjust(r.SignedAmount, r.AdminUserId, r.Note, DateTime.UtcNow);
        if (!isNew) _balances.Update(balance);           // a freshly-added balance is already tracked as Added
        await _balances.SaveChangesConcurrencySafeAsync(ct);

        return new ProviderBalanceAdjustResultDto(balance.ProviderProfileId, balance.Balance, balance.NegativeAmount);
    }
}
