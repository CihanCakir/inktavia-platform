using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Application.Commands.PrepareCargoDryKitRenewal;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryRenewalPreparationDetail;

[DocumentationInfo("GetCargoDryRenewalPreparationDetailQueryHandler",
    "Loads a renewal preparation by Id or RenewalCode and returns the enriched DTO. " +
    "Phase 11 (July 2026).")]
public sealed class GetCargoDryRenewalPreparationDetailQueryHandler
    : AizenQueryHandler<GetCargoDryRenewalPreparationDetailQuery, CargoDryRenewalPreparationDto?>
{
    private readonly ICargoDryRenewalPreparationRepository _preparations;
    private readonly ICargoDryProductRepository            _products;

    public GetCargoDryRenewalPreparationDetailQueryHandler(
        ICargoDryRenewalPreparationRepository preparations,
        ICargoDryProductRepository            products)
    {
        _preparations = preparations;
        _products     = products;
    }

    public override async Task<CargoDryRenewalPreparationDto?> Handle(
        GetCargoDryRenewalPreparationDetailQuery request, CancellationToken ct)
    {
        var preparation = request.Id.HasValue
            ? await _preparations.GetByIdAsync(request.Id.Value, ct)
            : request.RenewalCode is not null
                ? await _preparations.GetByRenewalCodeAsync(request.RenewalCode, ct)
                : null;

        if (preparation is null) return null;

        var product = await _products.GetByCodeAsync(preparation.ProductCode, ct);
        return PrepareCargoDryKitRenewalCommandHandler.MapToDto(preparation, product?.Name);
    }
}
