using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetPayoutDetail;

public sealed class GetPayoutDetailBffQuery : AizenQuery<GetPayoutDetailBffResponse>
{
    public long Id { get; init; }
}

public sealed class GetPayoutDetailBffResponse
{
    public PaymentPayoutBffDto? Payout { get; init; }
}
