using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetUpcomingPlanPriceChanges;

[DocumentationInfo("Get upcoming plan-price changes BFF query handler (BE-P4)",
    "Lists subscriptions whose renewal price changes within N days (GET /plan-prices/upcoming-changes). Read-only.")]
public sealed class GetUpcomingPlanPriceChangesBffQueryHandler
    : AizenQueryHandler<GetUpcomingPlanPriceChangesBffQuery, GetUpcomingPlanPriceChangesBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public GetUpcomingPlanPriceChangesBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<GetUpcomingPlanPriceChangesBffResponse?> Handle(GetUpcomingPlanPriceChangesBffQuery request, CancellationToken ct)
        => new() { Items = await _payment.GetUpcomingPlanPriceChangesAsync(request.WithinDays, ct) };
}
