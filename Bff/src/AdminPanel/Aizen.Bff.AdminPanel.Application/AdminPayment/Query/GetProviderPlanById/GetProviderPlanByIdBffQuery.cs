using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetProviderPlanById;

public sealed class GetProviderPlanByIdBffQuery : AizenQuery<ProviderPlanBffDto?>
{
    public long Id { get; init; }
}
