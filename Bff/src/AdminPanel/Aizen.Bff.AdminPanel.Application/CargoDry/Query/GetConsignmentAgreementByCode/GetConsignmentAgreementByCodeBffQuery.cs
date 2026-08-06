using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetConsignmentAgreementByCode;

public sealed class GetConsignmentAgreementByCodeBffQuery
    : AizenQuery<GetConsignmentAgreementByCodeBffResponse?>
{
    public string AgreementCode { get; init; } = default!;
}

public sealed class GetConsignmentAgreementByCodeBffResponse
{
    public ConsignmentAgreementBffDto Agreement { get; init; } = default!;
}
