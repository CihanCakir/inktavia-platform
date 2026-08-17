using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Commands.UpdateConsignmentAgreement;

public sealed class UpdateConsignmentAgreementCommand : AizenCommand<CargoDryConsignmentAgreementDto>
{
    public long     Id                      { get; init; }
    public decimal  ConsignmentRate         { get; init; }
    public decimal  MinimumSettlementAmount { get; init; }
    public string   CurrencyCode            { get; init; } = "TRY";
    public int      MaxKitCount             { get; init; }
    public DateTime StartDateUtc            { get; init; }
    public DateTime? EndDateUtc             { get; init; }
    public string?  TermsDocumentRef        { get; init; }
    public string?  Notes                   { get; init; }
}
