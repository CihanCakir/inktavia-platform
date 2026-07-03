using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetActiveConsignmentAgreementForProvider;

public sealed class GetActiveConsignmentAgreementForProviderBffQuery
    : AizenQuery<GetActiveConsignmentAgreementForProviderBffResponse?>
{
    public long   ProviderProfileId { get; init; }
    public string ProductCode       { get; init; } = default!;
}

public sealed class GetActiveConsignmentAgreementForProviderBffResponse
{
    public ConsignmentAgreementBffDto Agreement { get; init; } = default!;
}
