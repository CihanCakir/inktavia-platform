using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDrySellThroughSettlementsPaged;

public sealed class GetCargoDrySellThroughSettlementsPagedBffQuery
    : AizenQuery<GetCargoDrySellThroughSettlementsPagedBffResponse>
{
    public long?     ProviderProfileId      { get; init; }
    public long?     ConsignmentAgreementId { get; init; }
    public string?   ProductCode            { get; init; }
    public int?      Status                 { get; init; }
    public DateTime? PeriodFrom             { get; init; }
    public DateTime? PeriodTo               { get; init; }
    public string?   Search                 { get; init; }
    public int       Page                   { get; init; } = 1;
    public int       PageSize               { get; init; } = 25;
}

public sealed class GetCargoDrySellThroughSettlementsPagedBffResponse
{
    public CargoDrySellThroughSettlementPagedBffDto PagedResult { get; init; } = default!;
}
