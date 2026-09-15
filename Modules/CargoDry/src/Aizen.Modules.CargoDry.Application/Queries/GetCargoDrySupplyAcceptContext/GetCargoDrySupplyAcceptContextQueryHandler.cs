using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDrySupplyAcceptContext;

[DocumentationInfo("Get CargoDry supply accept-context query handler",
    "Returns the product's active/retail state + whether the provider holds an ACTIVE ConsignmentAgreement for it. " +
    "Consumed by ServiceRequest to gate a provider's accept of a CARGODRY_SUPPLY request and pin the offer price.")]
public sealed class GetCargoDrySupplyAcceptContextQueryHandler
    : AizenQueryHandler<GetCargoDrySupplyAcceptContextQuery, GetCargoDrySupplyAcceptContextResponse>
{
    private readonly ICargoDryProductRepository              _products;
    private readonly ICargoDryConsignmentAgreementRepository _agreements;
    private readonly ICargoDryKitRepository                  _kits;

    public GetCargoDrySupplyAcceptContextQueryHandler(
        ICargoDryProductRepository              products,
        ICargoDryConsignmentAgreementRepository agreements,
        ICargoDryKitRepository                  kits)
    {
        _products   = products;
        _agreements = agreements;
        _kits       = kits;
    }

    public override async Task<GetCargoDrySupplyAcceptContextResponse?> Handle(
        GetCargoDrySupplyAcceptContextQuery request, CancellationToken ct)
    {
        var product = await _products.GetByCodeAsync(request.ProductCode, ct);

        var hasAgreement = request.ProviderProfileId > 0
            && await _agreements.GetActiveForProviderProductAsync(
                   request.ProviderProfileId, request.ProductCode, DateTime.UtcNow, ct) is not null;

        // A1 — live available-kit stock gate (only meaningful when the provider is in the programme).
        var availableKits = request.ProviderProfileId > 0
            ? await _kits.CountAvailableForProviderProductAsync(request.ProviderProfileId, request.ProductCode, ct)
            : 0;

        return new GetCargoDrySupplyAcceptContextResponse
        {
            ProductActive              = product is { IsActive: true },
            RetailPrice                = product?.RetailPrice ?? 0m,
            CurrencyCode               = product?.CurrencyCode,
            ProviderHasActiveAgreement = hasAgreement,
            AvailableKitCount          = availableKits,
        };
    }
}
