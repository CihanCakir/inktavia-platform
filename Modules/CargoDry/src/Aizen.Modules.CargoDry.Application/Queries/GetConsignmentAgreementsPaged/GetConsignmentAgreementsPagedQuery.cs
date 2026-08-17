using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Application.Queries.GetConsignmentAgreementsPaged;

public sealed class GetConsignmentAgreementsPagedQuery
    : AizenQuery<CargoDryConsignmentAgreementPagedResultDto>
{
    public long?                       ProviderProfileId { get; init; }
    public string?                     ProductCode       { get; init; }
    public ConsignmentAgreementStatus? Status            { get; init; }
    public DateTime?                   DateFrom          { get; init; }
    public DateTime?                   DateTo            { get; init; }
    public string?                     Search            { get; init; }
    public int                         Page              { get; init; } = 1;
    public int                         PageSize          { get; init; } = 25;
}
