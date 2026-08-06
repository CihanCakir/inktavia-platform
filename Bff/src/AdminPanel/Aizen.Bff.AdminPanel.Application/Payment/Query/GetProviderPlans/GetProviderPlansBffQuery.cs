using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetProviderPlans;

public sealed class GetProviderPlansBffQuery : AizenQuery<GetProviderPlansBffResponse>
{
}

public sealed class GetProviderPlansBffResponse
{
    public List<ProviderPlanBffDto> Plans { get; init; } = [];
}
