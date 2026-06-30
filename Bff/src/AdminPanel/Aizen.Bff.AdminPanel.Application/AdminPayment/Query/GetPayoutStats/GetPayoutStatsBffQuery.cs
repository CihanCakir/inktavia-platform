using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetPayoutStats;

public sealed class GetPayoutStatsBffQuery : AizenQuery<GetPayoutStatsBffResponse>
{
}

public sealed class GetPayoutStatsBffResponse
{
    public PaymentPayoutStatsBffDto Stats { get; init; } = default!;
}
