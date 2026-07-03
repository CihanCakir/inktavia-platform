using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.UpdateConsignmentAgreement;

public sealed class UpdateConsignmentAgreementBffCommand
    : AizenCommand<UpdateConsignmentAgreementBffCommandResponse>
{
    public long      Id                      { get; init; }
    public decimal   ConsignmentRate         { get; init; }
    public decimal   MinimumSettlementAmount { get; init; }
    public string    CurrencyCode            { get; init; } = "TRY";
    public int       MaxKitCount             { get; init; }
    public DateTime  StartDateUtc            { get; init; }
    public DateTime? EndDateUtc              { get; init; }
    public string?   TermsDocumentRef        { get; init; }
    public string?   Notes                   { get; init; }
}

public sealed class UpdateConsignmentAgreementBffCommandResponse
{
    public ConsignmentAgreementBffDto Agreement { get; init; } = default!;
}
