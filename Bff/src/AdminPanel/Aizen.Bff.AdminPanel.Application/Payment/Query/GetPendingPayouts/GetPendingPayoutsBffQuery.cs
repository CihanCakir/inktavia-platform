using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetPendingPayouts;

public sealed class GetPendingPayoutsBffQuery : AizenQuery<GetPendingPayoutsBffResponse>
{
}

public sealed class GetPendingPayoutsBffResponse
{
    public List<PendingPayoutBffDto> Payouts { get; init; } = [];
}
