using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetConsignmentAgreementById;

public sealed class GetConsignmentAgreementByIdBffQuery
    : AizenQuery<GetConsignmentAgreementByIdBffResponse?>
{
    public long Id { get; init; }
}

public sealed class GetConsignmentAgreementByIdBffResponse
{
    public ConsignmentAgreementBffDto Agreement { get; init; } = default!;
}
