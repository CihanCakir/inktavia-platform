using Aizen.Bff.AdminPanel.Application.Vessels.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDryProductDetail;

[DocumentationInfo("Get CargoDry product detail BFF query handler",
    "Returns a single product with operational kit statistics from the CargoDry admin catalog.")]
public sealed class GetCargoDryProductDetailBffQueryHandler
    : AizenQueryHandler<GetCargoDryProductDetailBffQuery, GetCargoDryProductDetailBffResponse?>
{
    private readonly ICargoDryRemoteCall _remote;

    public GetCargoDryProductDetailBffQueryHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<GetCargoDryProductDetailBffResponse?> Handle(
        GetCargoDryProductDetailBffQuery request, CancellationToken ct)
    {
        var product = await _remote.GetProductDetailAsync(request.ProductCode, ct);
        if (product is null) return null;

        // Kit stats are now included in the module response — map them through if present.
        CargoDryProductKitStatsBffDto? kitStats = product.KitStats is { } ks
            ? new CargoDryProductKitStatsBffDto
            {
                TotalKitsIssued  = ks.TotalKitsIssued,
                ActiveKits       = ks.ActiveKits,
                ExpiredKits      = ks.ExpiredKits,
                RevokedKits      = ks.RevokedKits,
                RenewedKits      = ks.RenewedKits,
                AvgEfficiencyPct = ks.AvgEfficiencyPct,
                RenewalRatePct   = ks.RenewalRatePct,
                ExpiringIn30Days = ks.ExpiringIn30Days,
            }
            : null;

        var dto = new CargoDryProductBffDto
        {
            Id             = product.Id,
            ProductCode    = product.ProductCode,
            Name           = product.Name,
            Description    = product.Description,
            ValidityDays   = product.ValidityDays,
            HasSmartDevice = product.HasSmartDevice,
            DeviceType     = product.DeviceType,
            RetailPrice    = product.RetailPrice,
            CurrencyCode   = product.CurrencyCode,
            IsActive       = product.IsActive,
            CreatedAt      = product.CreatedAt,
            KitStats       = kitStats,
        };

        return new GetCargoDryProductDetailBffResponse { Product = dto };
    }
}
