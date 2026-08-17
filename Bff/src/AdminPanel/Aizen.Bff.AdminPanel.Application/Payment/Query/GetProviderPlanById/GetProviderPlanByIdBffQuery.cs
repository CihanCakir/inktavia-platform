using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetProviderPlanById;

public sealed class GetProviderPlanByIdBffQuery : AizenQuery<ProviderPlanBffDto?>
{
    public long Id { get; init; }
}
