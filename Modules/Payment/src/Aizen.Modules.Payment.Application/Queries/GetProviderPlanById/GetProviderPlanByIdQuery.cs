using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Modules.Payment.Application.Queries.GetProviderPlanById;

public sealed class GetProviderPlanByIdQuery : AizenQuery<ProviderPlanDto>
{
    public long Id { get; init; }
}
