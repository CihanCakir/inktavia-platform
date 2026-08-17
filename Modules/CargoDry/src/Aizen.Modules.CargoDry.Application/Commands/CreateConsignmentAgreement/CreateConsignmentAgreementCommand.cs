using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Commands.CreateConsignmentAgreement;

public sealed class CreateConsignmentAgreementCommand : AizenCommand<CargoDryConsignmentAgreementDto>
{
    public string   AgreementCode           { get; init; } = default!;
    public long     ProviderProfileId        { get; init; }
    public string   ProductCode              { get; init; } = default!;
    public decimal  ConsignmentRate          { get; init; }
    public decimal  MinimumSettlementAmount  { get; init; }
    public string   CurrencyCode             { get; init; } = "TRY";
    public int      MaxKitCount              { get; init; }
    public DateTime StartDateUtc             { get; init; }
    public DateTime? EndDateUtc             { get; init; }
    public string?  TermsDocumentRef         { get; init; }
    public string?  Notes                    { get; init; }
}
