using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Modules.Payment.Application.Queries.GetProviderPayouts;

public sealed class GetProviderPayoutsQuery : AizenQuery<ProviderPayoutPagedResultDto>
{
    public long ProviderProfileId { get; init; }
    public int? Status           { get; init; }
    public int  Page             { get; init; } = 1;
    public int  PageSize         { get; init; } = 20;
}
