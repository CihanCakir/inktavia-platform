using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetConsignmentAgreementById;

public sealed class GetConsignmentAgreementByIdQuery : AizenQuery<CargoDryConsignmentAgreementDto?>
{
    public long Id { get; init; }
}
