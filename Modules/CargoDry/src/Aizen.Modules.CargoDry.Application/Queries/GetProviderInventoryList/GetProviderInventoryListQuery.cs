using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Application.Queries.GetProviderInventoryList;

public sealed class GetProviderInventoryListQuery : AizenQuery<CargoDryProviderInventoryPagedResultDto>
{
    public long?                    ProviderProfileId { get; init; }
    public string?                  ProductCode       { get; init; }
    public CargoDryCommercialModel? CommercialModel   { get; init; }
    public SalesChannel?            SalesChannel      { get; init; }
    public bool?                    HasAvailableStock { get; init; }
    public string?                  Search            { get; init; }
    public int                      Page              { get; init; } = 1;
    public int                      PageSize          { get; init; } = 25;
}
