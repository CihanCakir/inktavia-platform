using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetConsignmentAgreementById;

public sealed class GetConsignmentAgreementByIdBffQuery
    : AizenQuery<GetConsignmentAgreementByIdBffResponse?>
{
    public long Id { get; init; }
}

public sealed class GetConsignmentAgreementByIdBffResponse
{
    public ConsignmentAgreementBffDto Agreement { get; init; } = default!;
}
