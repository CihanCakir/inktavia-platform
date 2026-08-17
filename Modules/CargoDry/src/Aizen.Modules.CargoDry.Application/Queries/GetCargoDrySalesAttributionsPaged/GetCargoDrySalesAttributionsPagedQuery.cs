using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDrySalesAttributionsPaged;

public sealed class GetCargoDrySalesAttributionsPagedQuery
    : AizenQuery<GetCargoDrySalesAttributionsPagedResponse>
{
    public long?                           ProviderProfileId      { get; init; }
    public string?                         ProductCode            { get; init; }
    public string?                         BatchCode              { get; init; }
    public SalesChannel?                   SalesChannel           { get; init; }
    public CargoDryCommercialModel?        CommercialModel        { get; init; }
    public CargoDrySalesAttributionStatus? Status                 { get; init; }
    public long?                           SellThroughSettlementId { get; init; }
    public DateTime?                       DateFrom               { get; init; }
    public DateTime?                       DateTo                 { get; init; }
    public string?                         Search                 { get; init; }
    public int                             Page                   { get; init; } = 1;
    public int                             PageSize               { get; init; } = 50;
}

public sealed class GetCargoDrySalesAttributionsPagedResponse
{
    public CargoDrySalesAttributionPagedResultDto PagedResult { get; init; } = default!;
}
