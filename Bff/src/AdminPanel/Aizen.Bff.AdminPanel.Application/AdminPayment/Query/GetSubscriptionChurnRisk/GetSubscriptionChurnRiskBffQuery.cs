using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetSubscriptionChurnRisk;

public sealed class GetSubscriptionChurnRiskBffQuery : AizenQuery<GetSubscriptionChurnRiskBffResponse>;

public sealed class GetSubscriptionChurnRiskBffResponse
{
    public SubscriptionChurnRiskBffDto Result { get; init; } = default!;
}
