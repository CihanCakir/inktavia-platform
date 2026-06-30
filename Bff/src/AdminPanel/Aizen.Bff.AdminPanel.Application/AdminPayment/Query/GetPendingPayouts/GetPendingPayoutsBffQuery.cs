using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetPendingPayouts;

public sealed class GetPendingPayoutsBffQuery : AizenQuery<GetPendingPayoutsBffResponse>
{
}

public sealed class GetPendingPayoutsBffResponse
{
    public List<PendingPayoutBffDto> Payouts { get; init; } = [];
}
