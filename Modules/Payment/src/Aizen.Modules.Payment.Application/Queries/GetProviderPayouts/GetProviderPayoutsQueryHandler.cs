using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Abstraction.Dto;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetProviderPayouts;

public sealed class GetProviderPayoutsQueryHandler
    : AizenQueryHandler<GetProviderPayoutsQuery, ProviderPayoutPagedResultDto>
{
    private readonly IPayoutRecordRepository _payouts;

    public GetProviderPayoutsQueryHandler(IPayoutRecordRepository payouts)
    {
        _payouts = payouts;
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

        return new ProviderPayoutPagedResultDto
        {
            Items = items.Select(x => new ProviderPayoutDto
            {
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
        };
    }
}
