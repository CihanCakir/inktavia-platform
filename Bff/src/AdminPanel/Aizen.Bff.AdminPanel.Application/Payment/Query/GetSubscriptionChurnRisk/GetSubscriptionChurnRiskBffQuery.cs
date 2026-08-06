using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetSubscriptionChurnRisk;

public sealed class GetSubscriptionChurnRiskBffQuery : AizenQuery<GetSubscriptionChurnRiskBffResponse>;

public sealed class GetSubscriptionChurnRiskBffResponse
{
    public SubscriptionChurnRiskBffDto Result { get; init; } = default!;
}
