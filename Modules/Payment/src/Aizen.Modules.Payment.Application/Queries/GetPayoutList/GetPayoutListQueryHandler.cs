using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetPayoutList;

[DocumentationInfo("GetPayoutListQueryHandler",
    "Returns a paged list of payout records with optional status/provider/date filters. " +
    "Used by the admin payout management screen.")]
public sealed class GetPayoutListQueryHandler
    : AizenQueryHandler<GetPayoutListQuery, PayoutListResult>
{
    private readonly IPayoutRecordRepository _payouts;

    public GetPayoutListQueryHandler(IPayoutRecordRepository payouts)
        => _payouts = payouts;

    public override async Task<PayoutListResult?> Handle(
        GetPayoutListQuery request, CancellationToken ct)
    {
        var skip = (request.Page - 1) * request.PageSize;

        var (items, total) = await _payouts.GetPagedAsync(
            request.Status,
            request.ProviderId,
            request.FromDate,
            request.ToDate,
            skip, request.PageSize, ct);

        var dtos = items.Select(p => new PayoutListItemDto(
            p.Id, p.PaymentTransactionId, p.ProviderProfileId,
            p.Amount, p.CurrencyCode, p.Status,
            p.GatewayPayoutId, p.HoldReason, p.AdminNote,
            p.RequestedAt, p.ProcessedAt, p.HeldAt)).ToList();

        return new PayoutListResult(dtos, total, request.Page, request.PageSize);
    }
}
