using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetConsignmentAgreementByCode;

public sealed class GetConsignmentAgreementByCodeQuery : AizenQuery<CargoDryConsignmentAgreementDto?>
{
    public string AgreementCode { get; init; } = default!;
}
