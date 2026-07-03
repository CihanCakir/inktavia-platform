using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetConsignmentAgreementByCode;

public sealed class GetConsignmentAgreementByCodeBffQuery
    : AizenQuery<GetConsignmentAgreementByCodeBffResponse?>
{
    public string AgreementCode { get; init; } = default!;
}

public sealed class GetConsignmentAgreementByCodeBffResponse
{
    public ConsignmentAgreementBffDto Agreement { get; init; } = default!;
}
