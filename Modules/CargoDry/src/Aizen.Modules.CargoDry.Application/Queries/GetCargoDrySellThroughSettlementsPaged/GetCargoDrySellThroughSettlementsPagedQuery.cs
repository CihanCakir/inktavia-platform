using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDrySellThroughSettlementsPaged;

public sealed class GetCargoDrySellThroughSettlementsPagedQuery
    : AizenQuery<GetCargoDrySellThroughSettlementsPagedResponse>
{
    public long?                                ProviderProfileId      { get; init; }
    public long?                                ConsignmentAgreementId { get; init; }
    public string?                              ProductCode            { get; init; }
    public CargoDrySellThroughSettlementStatus? Status                 { get; init; }
    public DateTime?                            PeriodFrom             { get; init; }
    public DateTime?                            PeriodTo               { get; init; }
    public string?                              Search                 { get; init; }
    public int                                  Page                   { get; init; } = 1;
    public int                                  PageSize               { get; init; } = 50;
}

public sealed class GetCargoDrySellThroughSettlementsPagedResponse
{
    public CargoDrySellThroughSettlementPagedResultDto PagedResult { get; init; } = default!;
}
