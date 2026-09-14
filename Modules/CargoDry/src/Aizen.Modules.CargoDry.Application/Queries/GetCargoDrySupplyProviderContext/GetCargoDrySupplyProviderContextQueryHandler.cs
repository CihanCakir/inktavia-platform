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

    public GetCargoDrySupplyProviderContextQueryHandler(
        ICargoDryProductRepository               products,
        ICargoDryConsignmentAgreementRepository  agreements,
        ICargoDryOwnerPreferredProviderRepository preferred)
    {
        _products   = products;
        _agreements = agreements;
        _preferred  = preferred;
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
        if (request.ProviderProfileId > 0)
        {
            foreach (var code in distinctCodes)
            {
                var product = await _products.GetByCodeAsync(code, ct);
                if (product is not { IsActive: true }) continue;
                var agreement = await _agreements.GetActiveForProviderProductAsync(request.ProviderProfileId, code, now, ct);
                if (agreement is not null) acceptable.Add(code);
            }
        }

        var preferredOwners = request.ProviderProfileId > 0
            ? (await _preferred.GetOwnerIdsPreferringProviderAsync(request.ProviderProfileId, ct)).ToList()
            : new List<long>();

        return new GetCargoDrySupplyProviderContextResponse
        {
            AcceptableProductCodes = acceptable,
            PreferredOwnerUserIds  = preferredOwners,
        };
    }
}
