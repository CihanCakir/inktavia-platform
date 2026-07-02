using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Application.Queries.GetProviderPlans;

namespace Aizen.Modules.Payment.Application.Queries.GetProviderPlanById;

public sealed class GetProviderPlanByIdQuery : AizenQuery<ProviderPlanDto>
{
    public long Id { get; init; }
}
