using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using static Aizen.Modules.CargoDry.Application.Commands.CreateConsignmentAgreement.CreateConsignmentAgreementCommandHandler;

namespace Aizen.Modules.CargoDry.Application.Queries.GetConsignmentAgreementByCode;

public sealed class GetConsignmentAgreementByCodeQueryHandler
    : AizenQueryHandler<GetConsignmentAgreementByCodeQuery, CargoDryConsignmentAgreementDto?>
{
    private readonly ICargoDryConsignmentAgreementRepository _agreements;

    public GetConsignmentAgreementByCodeQueryHandler(
        ICargoDryConsignmentAgreementRepository agreements)
    {
        _agreements = agreements;
    }

    public override async Task<CargoDryConsignmentAgreementDto?> Handle(
        GetConsignmentAgreementByCodeQuery request, CancellationToken ct)
    {
        var entity = await _agreements.GetByAgreementCodeAsync(request.AgreementCode, ct);
        return entity is null ? null : MapToDto(entity);
    }
}
