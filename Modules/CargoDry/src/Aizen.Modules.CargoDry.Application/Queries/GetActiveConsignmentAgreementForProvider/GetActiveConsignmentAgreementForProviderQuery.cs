using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetActiveConsignmentAgreementForProvider;

public sealed class GetActiveConsignmentAgreementForProviderQuery
    : AizenQuery<CargoDryConsignmentAgreementDto?>
{
    public long   ProviderProfileId { get; init; }
    public string ProductCode       { get; init; } = default!;
}
