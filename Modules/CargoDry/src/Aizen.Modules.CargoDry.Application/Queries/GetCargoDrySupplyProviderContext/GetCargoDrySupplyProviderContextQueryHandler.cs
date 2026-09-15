using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDrySupplyProviderContext;

[DocumentationInfo("Get CargoDry supply provider-context query handler",
    "Batch context for a provider discovery page: acceptable (active-agreement + active-product) product codes + the " +
    "owner ids that prefer this provider. Consumed by the provider BFF to compute canAccept + isPreferred.")]
public sealed class GetCargoDrySupplyProviderContextQueryHandler
    : AizenQueryHandler<GetCargoDrySupplyProviderContextQuery, GetCargoDrySupplyProviderContextResponse>
{
    private readonly ICargoDryProductRepository               _products;
    private readonly ICargoDryConsignmentAgreementRepository  _agreements;
    private readonly ICargoDryOwnerPreferredProviderRepository _preferred;
    private readonly ICargoDryKitRepository                   _kits;

    public GetCargoDrySupplyProviderContextQueryHandler(
        ICargoDryProductRepository               products,
        ICargoDryConsignmentAgreementRepository  agreements,
        ICargoDryOwnerPreferredProviderRepository preferred,
        ICargoDryKitRepository                   kits)
    {
        _products   = products;
        _agreements = agreements;
        _preferred  = preferred;
        _kits       = kits;
    }

    public override async Task<GetCargoDrySupplyProviderContextResponse?> Handle(
        GetCargoDrySupplyProviderContextQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var distinctCodes = request.ProductCodes
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim().ToUpperInvariant())
            .Distinct()
            .ToList();

        var acceptable = new List<string>();
        var productStock = new List<CargoDrySupplyProductStockDto>();
        if (request.ProviderProfileId > 0)
        {
            foreach (var code in distinctCodes)
            {
                var product = await _products.GetByCodeAsync(code, ct);
                if (product is not { IsActive: true }) continue;
                var hasAgreement = await _agreements.GetActiveForProviderProductAsync(request.ProviderProfileId, code, now, ct) is not null;
                // A1 — count available stock only when in-programme (no agreement ⇒ locked regardless of stock).
                var availableKits = hasAgreement
                    ? await _kits.CountAvailableForProviderProductAsync(request.ProviderProfileId, code, ct)
                    : 0;
                productStock.Add(new CargoDrySupplyProductStockDto
                {
                    ProductCode = code, HasActiveAgreement = hasAgreement, AvailableKitCount = availableKits,
                });
                // Acceptable now requires agreement AND available stock.
                if (hasAgreement && availableKits > 0) acceptable.Add(code);
            }
        }

        var preferredOwners = request.ProviderProfileId > 0
            ? (await _preferred.GetOwnerIdsPreferringProviderAsync(request.ProviderProfileId, ct)).ToList()
            : new List<long>();

        return new GetCargoDrySupplyProviderContextResponse
        {
            AcceptableProductCodes = acceptable,
            PreferredOwnerUserIds  = preferredOwners,
            ProductStock           = productStock,
        };
    }
}
