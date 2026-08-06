using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetSubscriptionMrrTrend;

public sealed class GetSubscriptionMrrTrendBffQuery : AizenQuery<GetSubscriptionMrrTrendBffResponse>
{
    public int Months { get; init; } = 6;
}

public sealed class GetSubscriptionMrrTrendBffResponse
{
    public SubscriptionMrrTrendBffResult Result { get; init; } = default!;
}
