using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetSubscriptionStats;

public sealed class GetSubscriptionStatsBffQuery : AizenQuery<GetSubscriptionStatsBffResponse>
{
}

public sealed class GetSubscriptionStatsBffResponse
{
    public SubscriptionStatsBffDto Stats { get; init; } = default!;
}
