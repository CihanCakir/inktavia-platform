using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetProviderPlans;

public sealed class GetProviderPlansBffQuery : AizenQuery<GetProviderPlansBffResponse>
{
}

public sealed class GetProviderPlansBffResponse
{
    public List<ProviderPlanBffDto> Plans { get; init; } = [];
}
