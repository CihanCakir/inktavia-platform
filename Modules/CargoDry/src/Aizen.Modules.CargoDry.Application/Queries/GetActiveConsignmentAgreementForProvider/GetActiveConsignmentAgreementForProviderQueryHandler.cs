using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using static Aizen.Modules.CargoDry.Application.Commands.CreateConsignmentAgreement.CreateConsignmentAgreementCommandHandler;

namespace Aizen.Modules.CargoDry.Application.Queries.GetActiveConsignmentAgreementForProvider;

public sealed class GetActiveConsignmentAgreementForProviderQueryHandler
    : AizenQueryHandler<GetActiveConsignmentAgreementForProviderQuery, CargoDryConsignmentAgreementDto?>
{
    private readonly ICargoDryConsignmentAgreementRepository _agreements;

    public GetActiveConsignmentAgreementForProviderQueryHandler(
        ICargoDryConsignmentAgreementRepository agreements)
    {
        _agreements = agreements;
    }

    public override async Task<CargoDryConsignmentAgreementDto?> Handle(
        GetActiveConsignmentAgreementForProviderQuery request, CancellationToken ct)
    {
        var nowUtc = DateTime.UtcNow;
        var entity = await _agreements.GetActiveForProviderProductAsync(
            request.ProviderProfileId, request.ProductCode, nowUtc, ct);
        return entity is null ? null : MapToDto(entity);
    }
}
