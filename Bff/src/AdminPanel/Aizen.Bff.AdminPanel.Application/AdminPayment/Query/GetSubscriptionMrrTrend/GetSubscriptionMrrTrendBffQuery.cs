using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetSubscriptionMrrTrend;

public sealed class GetSubscriptionMrrTrendBffQuery : AizenQuery<GetSubscriptionMrrTrendBffResponse>
{
    public int Months { get; init; } = 6;
}

public sealed class GetSubscriptionMrrTrendBffResponse
{
    public SubscriptionMrrTrendBffResult Result { get; init; } = default!;
}
