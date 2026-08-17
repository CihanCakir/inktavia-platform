using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDrySalesAttributionsPaged;

public sealed class GetCargoDrySalesAttributionsPagedBffQuery
    : AizenQuery<GetCargoDrySalesAttributionsPagedBffResponse>
{
    public long?     ProviderProfileId       { get; init; }
    public string?   ProductCode             { get; init; }
    public string?   BatchCode               { get; init; }
    public int?      SalesChannel            { get; init; }
    public int?      CommercialModel         { get; init; }
    public int?      Status                  { get; init; }
    public long?     SellThroughSettlementId { get; init; }
    public DateTime? DateFrom                { get; init; }
    public DateTime? DateTo                  { get; init; }
    public string?   Search                  { get; init; }
    public int       Page                    { get; init; } = 1;
    public int       PageSize                { get; init; } = 25;
}

public sealed class GetCargoDrySalesAttributionsPagedBffResponse
{
    public CargoDrySalesAttributionPagedBffDto PagedResult { get; init; } = default!;
}
