using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using static Aizen.Modules.CargoDry.Application.Commands.CreateConsignmentAgreement.CreateConsignmentAgreementCommandHandler;

namespace Aizen.Modules.CargoDry.Application.Queries.GetConsignmentAgreementById;

public sealed class GetConsignmentAgreementByIdQueryHandler
    : AizenQueryHandler<GetConsignmentAgreementByIdQuery, CargoDryConsignmentAgreementDto?>
{
    private readonly ICargoDryConsignmentAgreementRepository _agreements;

    public GetConsignmentAgreementByIdQueryHandler(
        ICargoDryConsignmentAgreementRepository agreements)
    {
        _agreements = agreements;
    }

    public override async Task<CargoDryConsignmentAgreementDto?> Handle(
        GetConsignmentAgreementByIdQuery request, CancellationToken ct)
    {
        var entity = await _agreements.GetByIdAsync(request.Id, ct);
        return entity is null ? null : MapToDto(entity);
    }
}
