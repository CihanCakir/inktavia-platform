using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Modules.Payment.Application.Queries.GetProviderPayoutSummary;

public sealed class GetProviderPayoutSummaryQuery : AizenQuery<ProviderPayoutSummaryDto>
{
    public long ProviderProfileId { get; init; }
}
