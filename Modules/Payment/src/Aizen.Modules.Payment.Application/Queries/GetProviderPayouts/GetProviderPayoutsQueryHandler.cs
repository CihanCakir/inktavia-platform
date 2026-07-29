using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Abstraction.Dto;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetProviderPayouts;

public sealed class GetProviderPayoutsQueryHandler
    : AizenQueryHandler<GetProviderPayoutsQuery, ProviderPayoutPagedResultDto>
{
    private readonly IPayoutRecordRepository    _payouts;
    private readonly IProviderBalanceRepository _balances;

    public GetProviderPayoutsQueryHandler(IPayoutRecordRepository payouts, IProviderBalanceRepository balances)
    {
        _payouts  = payouts;
        _balances = balances;
    }

    public override async Task<ProviderPayoutPagedResultDto?> Handle(
        GetProviderPayoutsQuery request, CancellationToken ct)
    {
        var skip = (request.Page - 1) * request.PageSize;
        var status = request.Status.HasValue
            ? (PayoutStatus?)request.Status.Value
            : null;

        var (items, total) = await _payouts.GetPagedAsync(
            status: status,
            providerProfileId: request.ProviderProfileId,
            fromDate: null,
            toDate: null,
            skip: skip,
            take: request.PageSize,
            ct: ct);

        // ── BE-P10: the provider's negative-balance ledger state (TRY), for clawback/offset transparency. Null when none. ──
        var balance = await _balances.GetByProviderAsync(request.ProviderProfileId, "TRY", ct);
        var negativeBalance = balance is null ? null : new ProviderBalanceSummaryDto
        {
            Balance              = balance.Balance,
            NegativeAmount       = balance.NegativeAmount,
            NegativeBalanceLimit = balance.NegativeBalanceLimit,
            IsOverLimit          = balance.IsOverLimit(),
            CurrencyCode         = balance.CurrencyCode,
        };

        return new ProviderPayoutPagedResultDto
        {
            Items = items.Select(x => new ProviderPayoutDto
            {
                Id              = x.Id,
                Amount          = x.Amount,
                CurrencyCode    = x.CurrencyCode,
                Status          = (int)x.Status,
                GatewayProvider = x.GatewayProvider,
                GatewayPayoutId = x.GatewayPayoutId,
                SourceType      = x.SourceType,
                SourceId        = x.SourceId,
                Description     = x.Description,
                RequestedAt     = x.RequestedAt,
                ProcessedAt     = x.ProcessedAt,
                HoldReason      = x.HoldReason,
                FailureReason   = x.FailureReason,
            }).ToList(),
            Total    = total,
            Page     = request.Page,
            PageSize = request.PageSize,
            NegativeBalance = negativeBalance,
        };
    }
}
