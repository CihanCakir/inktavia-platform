using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDryInventoryList;

public sealed class GetCargoDryInventoryListBffQuery : AizenQuery<GetCargoDryInventoryListBffResponse>
{
    public long?   ProviderProfileId { get; init; }
    public string? ProductCode       { get; init; }
    public int?    CommercialModel   { get; init; }
    public int?    SalesChannel      { get; init; }
    public bool?   HasAvailableStock { get; init; }
    public string? Search            { get; init; }
    public int     Page              { get; init; } = 1;
    public int     PageSize          { get; init; } = 25;
}

public sealed class GetCargoDryInventoryListBffResponse
{
    public CargoDryProviderInventoryPagedBffDto? PagedResult { get; init; }
}
