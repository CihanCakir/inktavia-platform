using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryProviderSettlements;

public sealed class GetCargoDryProviderSettlementsQuery : AizenQuery<CargoDryProviderSettlementPagedResultDto>
{
    public long ProviderProfileId { get; init; }
    public int? Status           { get; init; }
    public int  Page             { get; init; } = 1;
    public int  PageSize         { get; init; } = 20;
    public DateTime? From { get; init; }
    public DateTime? To   { get; init; }
}
