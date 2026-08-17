using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryProviderPayoutSummary;

public sealed class GetCargoDryProviderPayoutSummaryQuery : AizenQuery<CargoDryProviderPayoutSummaryDto>
{
    public long ProviderProfileId { get; init; }
}
