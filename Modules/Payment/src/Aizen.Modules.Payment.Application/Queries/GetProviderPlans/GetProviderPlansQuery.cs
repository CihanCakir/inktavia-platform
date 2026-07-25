using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Modules.Payment.Application.Queries.GetProviderPlans;

public sealed class GetProviderPlansQuery : AizenQuery<List<ProviderPlanDto>>
{
    public long ProviderProfileId { get; init; }
}
