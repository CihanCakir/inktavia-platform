using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Modules.Payment.Application.Queries.GetProviderSubscription;

public sealed class GetProviderSubscriptionQuery : AizenQuery<ProviderSubscriptionDto>
{
    public long ProviderProfileId { get; init; }
}
