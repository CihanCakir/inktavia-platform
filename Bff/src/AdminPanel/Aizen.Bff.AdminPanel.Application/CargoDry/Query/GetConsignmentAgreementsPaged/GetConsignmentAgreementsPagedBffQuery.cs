using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetConsignmentAgreementsPaged;

public sealed class GetConsignmentAgreementsPagedBffQuery
    : AizenQuery<GetConsignmentAgreementsPagedBffResponse>
{
    public long?     ProviderProfileId { get; init; }
    public string?   ProductCode       { get; init; }
    public int?      Status            { get; init; }
    public DateTime? DateFrom          { get; init; }
    public DateTime? DateTo            { get; init; }
    public string?   Search            { get; init; }
    public int       Page              { get; init; } = 1;
    public int       PageSize          { get; init; } = 25;
}

public sealed class GetConsignmentAgreementsPagedBffResponse
{
    public ConsignmentAgreementPagedBffDto PagedResult { get; init; } = default!;
}
